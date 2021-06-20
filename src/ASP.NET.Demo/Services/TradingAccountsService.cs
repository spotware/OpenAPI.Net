using ASP.NET.Demo.Models;
using Google.Protobuf;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Services
{
    public interface ITradingAccountsService
    {
        long GetAccountId(long login);

        ProtoOACtidTraderAccount GetAccountById(long id);

        ProtoOACtidTraderAccount GetAccountByLogin(long login);

        Task<AccountModel> GetAccountModelById(long id);

        Task<AccountModel> GetAccountModelByLogin(long login);

        Task<IEnumerable<ProtoOACtidTraderAccount>> GetAccounts(string accessToken);
    }

    public class TradingAccountsService : ITradingAccountsService
    {
        private readonly IOpenApiService _apiService;

        private readonly ConcurrentDictionary<long, ProtoOACtidTraderAccount> _accounts = new();

        private readonly ConcurrentDictionary<long, long> accountIds = new();

        private readonly ConcurrentDictionary<long, AccountModel> _accountModels = new();

        public TradingAccountsService(IOpenApiService apiService)
        {
            _apiService = apiService;

            _apiService.Connected += ApiServiceConnected;
        }

        private void ApiServiceConnected()
        {
            Subscribe(_apiService.LiveObservable);
            Subscribe(_apiService.DemoObservable);
        }

        public long GetAccountId(long login) => accountIds[login];

        public ProtoOACtidTraderAccount GetAccountById(long id) => _accounts[id];

        public ProtoOACtidTraderAccount GetAccountByLogin(long login) => _accounts[GetAccountId(login)];

        public async Task<AccountModel> GetAccountModelById(long id)
        {
            var account = GetAccountById(id);

            if (_accountModels.TryGetValue(id, out var model)) return model;

            var trader = await _apiService.GetTrader(id, account.IsLive);
            var assets = await _apiService.GetAssets(id, account.IsLive);
            var lightSymbols = await _apiService.GetLightSymbols(id, account.IsLive);

            model = new AccountModel
            {
                Id = id,
                IsLive = account.IsLive,
                Symbols = await _apiService.GetSymbolModels(id, account.IsLive, lightSymbols, assets),
                Trader = trader,
                RegistrationTime = DateTimeOffset.FromUnixTimeMilliseconds(trader.RegistrationTimestamp),
                Balance = MonetaryConverter.FromMonetary(trader.Balance),
                Assets = new ReadOnlyCollection<ProtoOAAsset>(assets),
                DepositAsset = assets.First(iAsset => iAsset.AssetId == trader.DepositAssetId)
            };

            await FillConversionSymbols(model);

            await FillAccountOrders(model);

            _accountModels.TryAdd(id, model);

            var symbolIds = model.Symbols.Select(iSymbol => iSymbol.Id).ToArray();

            await _apiService.SubscribeToSpots(id, account.IsLive, symbolIds);

            return model;
        }

        public Task<AccountModel> GetAccountModelByLogin(long login) => GetAccountModelById(GetAccountId(login));

        public async Task<IEnumerable<ProtoOACtidTraderAccount>> GetAccounts(string accessToken)
        {
            var accounts = await _apiService.GetAccountsList(accessToken);

            await AuthorizeAccounts(accounts, accessToken);

            return accounts;
        }

        private async Task FillConversionSymbols(AccountModel account)
        {
            foreach (var symbol in account.Symbols)
            {
                if (symbol.QuoteAsset.AssetId != account.DepositAsset.AssetId)
                {
                    var conversionLightSymbols = await _apiService.GetConversionSymbols(account.Id, account.IsLive, symbol.QuoteAsset.AssetId, account.DepositAsset.AssetId);

                    var conversionSymbolModels = conversionLightSymbols.Select(iLightSymbol => account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == iLightSymbol.SymbolId)).Where(iSymbol => iSymbol is not null);

                    symbol.ConversionSymbols.AddRange(conversionSymbolModels);
                }
                else
                {
                    symbol.ConversionSymbols.Add(symbol);
                }
            }
        }

        private async Task FillAccountOrders(AccountModel account)
        {
            var response = await _apiService.GetAccountOrders(account.Id, account.IsLive);

            var positions = from poisiton in response.Position
                            let positionSymbol = account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == poisiton.TradeData.SymbolId)
                            select new MarketOrderModel(poisiton, positionSymbol);

            var pendingOrders = from order in response.Order
                                where order.OrderType is ProtoOAOrderType.Limit or ProtoOAOrderType.Stop or ProtoOAOrderType.StopLimit
                                let orderSymbol = account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == order.TradeData.SymbolId)
                                select new PendingOrderModel(order, orderSymbol);

            account.Positions.AddRange(positions);
            account.PendingOrders.AddRange(pendingOrders);
        }

        private void OnSpotEvent(ProtoOASpotEvent spotEvent)
        {
            if (_accountModels.TryGetValue(spotEvent.CtidTraderAccountId, out var model) == false) return;

            var symbol = model.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == spotEvent.SymbolId);

            if (symbol is null) return;

            double bid = symbol.Bid;
            double ask = symbol.Ask;

            if (spotEvent.HasBid) bid = symbol.Data.GetPriceFromRelative((long)spotEvent.Bid);
            if (spotEvent.HasAsk) ask = symbol.Data.GetPriceFromRelative((long)spotEvent.Ask);

            symbol.OnTick(bid, ask);

            if (symbol.QuoteAsset.AssetId == model.DepositAsset.AssetId && symbol.TickValue is 0)
            {
                symbol.TickValue = symbol.Data.GetTickValue(symbol.QuoteAsset, model.DepositAsset, null);
            }
            else if (symbol.ConversionSymbols.Count > 0 && symbol.ConversionSymbols.All(iSymbol => iSymbol.Bid is not 0))
            {
                symbol.TickValue = symbol.Data.GetTickValue(symbol.QuoteAsset, model.DepositAsset, symbol.ConversionSymbols.Select(iSymbol => new Tuple<ProtoOAAsset, ProtoOAAsset, double>(iSymbol.BaseAsset, iSymbol.QuoteAsset, iSymbol.Bid)));
            }

            if (symbol.TickValue is not 0)
            {
                var positions = model.Positions.ToArray();

                foreach (var position in positions)
                {
                    if (position.Symbol != symbol) continue;

                    position.OnSymbolTick();
                }

                model.UpdateStatus();
            }
        }

        private void OnExecutionEvent(ProtoOAExecutionEvent executionEvent)
        {
            if (_accountModels.TryGetValue(executionEvent.CtidTraderAccountId, out var model) == false) return;

            var position = model.Positions.FirstOrDefault(iPoisition => iPoisition.Id == executionEvent.Order.PositionId);
            var order = model.PendingOrders.FirstOrDefault(iOrder => iOrder.Id == executionEvent.Order.OrderId);

            var symbol = model.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == executionEvent.Order.TradeData.SymbolId);

            if (symbol is null) return;

            switch (executionEvent.ExecutionType)
            {
                case ProtoOAExecutionType.OrderFilled or ProtoOAExecutionType.OrderPartialFill:
                    if (position is not null)
                    {
                        position.Update(executionEvent.Position, position.Symbol);

                        if (position.Volume is 0) model.Positions.Remove(position);
                    }
                    else
                    {
                        model.Positions.Add(new MarketOrderModel(executionEvent.Position, symbol));
                    }

                    if (order is not null) model.PendingOrders.Remove(order);

                    break;

                case ProtoOAExecutionType.OrderCancelled:
                    if (order is not null) model.PendingOrders.Remove(order);
                    if (position is not null && executionEvent.Order.OrderType == ProtoOAOrderType.StopLossTakeProfit) position.Update(executionEvent.Position, position.Symbol);
                    break;

                case ProtoOAExecutionType.OrderAccepted when (executionEvent.Order.OrderType == ProtoOAOrderType.StopLossTakeProfit):
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    if (order is not null) order.Update(executionEvent.Order, symbol);

                    break;

                case ProtoOAExecutionType.OrderAccepted when (executionEvent.Order.OrderStatus != ProtoOAOrderStatus.OrderStatusFilled
                    && executionEvent.Order.OrderType == ProtoOAOrderType.Limit
                    || executionEvent.Order.OrderType == ProtoOAOrderType.Stop
                    || executionEvent.Order.OrderType == ProtoOAOrderType.StopLimit):
                    model.PendingOrders.Add(new PendingOrderModel(executionEvent.Order, symbol));

                    break;

                case ProtoOAExecutionType.OrderReplaced:
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    if (order is not null) order.Update(executionEvent.Order, symbol);
                    break;

                case ProtoOAExecutionType.Swap:
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    break;
            }
        }

        private async Task AuthorizeAccounts(IEnumerable<ProtoOACtidTraderAccount> accounts, string accessToken)
        {
            foreach (var account in accounts)
            {
                var accountId = Convert.ToInt64(account.CtidTraderAccountId);

                await _apiService.AuthorizeAccount(accountId, account.IsLive, accessToken);

                _accounts.TryAdd(accountId, account);
                accountIds.TryAdd(account.TraderLogin, accountId);
            }
        }

        private void Subscribe(IObservable<IMessage> observable)
        {
            observable.OfType<ProtoOASpotEvent>().Subscribe(OnSpotEvent);

            observable.OfType<ProtoOAExecutionEvent>().Subscribe(OnExecutionEvent);
        }
    }
}