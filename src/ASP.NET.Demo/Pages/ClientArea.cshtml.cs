using ASP.NET.Demo.Models;
using ASP.NET.Demo.Services;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Pages
{
    public class ClientAreaModel : PageModel
    {
        private readonly ApiService _apiService;

        public ClientAreaModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public Token Token { get; set; }

        public ProtoOACtidTraderAccount[] Accounts { get; set; }

        public ProtoOACtidTraderAccount SelectedAccount { get; set; }

        public AccountModel AccountModel { get; set; }

        public async Task OnGetAsync()
        {
            if (!TempData.TryGetValue("Token", out var tokenJson)) return;

            Token = JsonSerializer.Deserialize<Token>(tokenJson.ToString());

            await _apiService.Connect();

            SubscribeToErrors(_apiService.LiveObservable);
            SubscribeToErrors(_apiService.DemoObservable);

            Accounts = await _apiService.GetAccountsList(Token.AccessToken);

            foreach (var account in Accounts)
            {
                await _apiService.AuthorizeAccount((long)account.CtidTraderAccountId, account.IsLive, Token.AccessToken);
            }

            SelectedAccount = Accounts.FirstOrDefault();

            await OnAccountChanged();
        }

        private void SubscribeToErrors(IObservable<IMessage> observable)
        {
            if (observable is null) throw new ArgumentNullException(nameof(observable));

            observable.Subscribe(_ => { }, OnError);
            observable.OfType<ProtoErrorRes>().Subscribe(OnErrorRes);
            observable.OfType<ProtoOAErrorRes>().Subscribe(OnOaErrorRes);
            observable.OfType<ProtoOAOrderErrorEvent>().Subscribe(OnOrderErrorRes);
            observable.OfType<ProtoOASpotEvent>().Subscribe(OnSpotEvent);
            observable.OfType<ProtoOAExecutionEvent>().Subscribe(OnExecutionEvent);
        }

        private void OnExecutionEvent(ProtoOAExecutionEvent executionEvent)
        {
            var accountModel = AccountModel;

            if (accountModel is null || executionEvent.CtidTraderAccountId != accountModel.Id) return;

            var position = accountModel.Positions.FirstOrDefault(iPoisition => iPoisition.Id == executionEvent.Order.PositionId);
            var order = accountModel.PendingOrders.FirstOrDefault(iOrder => iOrder.Id == executionEvent.Order.OrderId);

            var symbol = accountModel.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == executionEvent.Order.TradeData.SymbolId);

            if (symbol is null) return;

            switch (executionEvent.ExecutionType)
            {
                case ProtoOAExecutionType.OrderFilled or ProtoOAExecutionType.OrderPartialFill:
                    if (position is not null)
                    {
                        position.Update(executionEvent.Position, position.Symbol);

                        if (position.Volume is 0) AccountModel.Positions.Remove(position);
                    }
                    else
                    {
                        accountModel.Positions.Add(new MarketOrderModel(executionEvent.Position, symbol));
                    }

                    if (order is not null) accountModel.PendingOrders.Remove(order);

                    break;

                case ProtoOAExecutionType.OrderCancelled:
                    if (order is not null) accountModel.PendingOrders.Remove(order);
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
                    accountModel.PendingOrders.Add(new PendingOrderModel(executionEvent.Order, symbol));

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

        private void OnSpotEvent(ProtoOASpotEvent spotEvent)
        {
            var accountModel = AccountModel;

            if (accountModel is null || spotEvent.CtidTraderAccountId != accountModel.Id) return;

            var symbol = accountModel.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == spotEvent.SymbolId);

            if (symbol is null) return;

            double bid = symbol.Bid;
            double ask = symbol.Ask;

            if (spotEvent.HasBid) bid = symbol.Data.GetPriceFromRelative((long)spotEvent.Bid);
            if (spotEvent.HasAsk) ask = symbol.Data.GetPriceFromRelative((long)spotEvent.Ask);

            symbol.OnTick(bid, ask);

            if (symbol.QuoteAsset.AssetId == accountModel.DepositAsset.AssetId && symbol.TickValue is 0)
            {
                symbol.TickValue = symbol.Data.GetTickValue(symbol.QuoteAsset, accountModel.DepositAsset, null);
            }
            else if (symbol.ConversionSymbols.Count > 0 && symbol.ConversionSymbols.All(iSymbol => iSymbol.Bid is not 0))
            {
                symbol.TickValue = symbol.Data.GetTickValue(symbol.QuoteAsset, accountModel.DepositAsset, symbol.ConversionSymbols.Select(iSymbol => new Tuple<ProtoOAAsset, ProtoOAAsset, double>(iSymbol.BaseAsset, iSymbol.QuoteAsset, iSymbol.Bid)));
            }

            if (symbol.TickValue is not 0)
            {
                var positions = accountModel.Positions.ToArray();

                foreach (var position in positions)
                {
                    if (position.Symbol != symbol) continue;

                    position.OnSymbolTick();
                }

                AccountModel.UpdateStatus();
            }
        }

        private async void OnError(Exception exception)
        {
        }

        private async void OnOrderErrorRes(ProtoOAOrderErrorEvent error)
        {
        }

        private async void OnOaErrorRes(ProtoOAErrorRes error)
        {
        }

        private async void OnErrorRes(ProtoErrorRes error)
        {
        }

        private async Task OnAccountChanged()
        {
            if (AccountModel is not null)
            {
                var oldAccountSymbolIds = AccountModel.Symbols.Select(iSymbol => iSymbol.Id).ToArray();

                await _apiService.UnsubscribeFromSpots(AccountModel.Id, AccountModel.IsLive, oldAccountSymbolIds);
            }

            if (SelectedAccount is null) return;

            var accountId = (long)SelectedAccount.CtidTraderAccountId;
            var trader = await _apiService.GetTrader(accountId, SelectedAccount.IsLive);
            var assets = await _apiService.GetAssets(accountId, SelectedAccount.IsLive);
            var lightSymbols = await _apiService.GetLightSymbols(accountId, SelectedAccount.IsLive);

            AccountModel = new AccountModel
            {
                Id = accountId,
                IsLive = SelectedAccount.IsLive,
                Symbols = await _apiService.GetSymbolModels(accountId, SelectedAccount.IsLive, lightSymbols, assets),
                Trader = trader,
                RegistrationTime = DateTimeOffset.FromUnixTimeMilliseconds(trader.RegistrationTimestamp),
                Balance = MonetaryConverter.FromMonetary(trader.Balance),
                Assets = new ReadOnlyCollection<ProtoOAAsset>(assets),
                DepositAsset = assets.First(iAsset => iAsset.AssetId == trader.DepositAssetId)
            };

            await FillConversionSymbols(AccountModel);

            await FillAccountOrders(AccountModel);

            var symbolIds = AccountModel.Symbols.Select(iSymbol => iSymbol.Id).ToArray();

            await _apiService.SubscribeToSpots(accountId, AccountModel.IsLive, symbolIds);
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
    }
}