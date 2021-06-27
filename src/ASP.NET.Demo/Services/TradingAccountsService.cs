using ASP.NET.Demo.Models;
using Google.Protobuf;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Channels;
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

        Channel<SymbolQuote> GetSymbolsQuoteChannel(long accountId);

        void StopSymbolQuotes(long accountId);

        Channel<Position> GetPositionUpdatesChannel(long accountId);

        void StopPositionUpdates(long accountId);

        Task ClosePosition(long accountId, long positionId);

        Task CloseAllPosition(long accountId);

        Channel<Error> GetErrorsChannel(long accountId);

        void StopErrors(long accountId);

        Channel<PendingOrder> GetOrderUpdatesChannel(long accountId);

        void StopOrderUpdates(long accountId);

        Task CancelOrder(long accountId, long orderId);

        Task CancelAllOrders(long accountId);

        Channel<AccountInfo> GetAccountInfoUpdatesChannel(long accountId);

        void StopAccountInfoUpdates(long accountId);

        AccountInfo GetAccountInfo(long accountId);

        Task CreateNewMarketOrder(NewMarketOrderRequest orderRequest);

        Task ModifyMarketOrder(ModifyMarketOrderRequest orderRequest);

        Task CreateNewPendingOrder(NewPendingOrderRequest orderRequest);

        Task ModifyPendingOrder(ModifyPendingOrderRequest orderRequest);

        Task<IEnumerable<HistoricalTrade>> GetHistory(DateTimeOffset from, DateTimeOffset to, long accountId);

        Task<IEnumerable<Transaction>> GetTransactions(DateTimeOffset from, DateTimeOffset to, long accountId);
    }

    public class TradingAccountsService : ITradingAccountsService
    {
        private readonly IOpenApiService _apiService;

        private readonly ConcurrentDictionary<long, ProtoOACtidTraderAccount> _accounts = new();

        private readonly ConcurrentDictionary<long, long> _accountIds = new();

        private readonly ConcurrentDictionary<long, AccountModel> _accountModels = new();

        private readonly ConcurrentDictionary<long, Channel<SymbolQuote>> _subscribedAccountQuoteChannels = new();

        private readonly ConcurrentDictionary<long, Channel<Position>> _subscribedAccountPositionUpdateChannels = new();

        private readonly ConcurrentDictionary<long, Channel<PendingOrder>> _subscribedAccountOrderUpdateChannels = new();

        private readonly ConcurrentDictionary<long, Channel<Error>> _subscribedAccountErrorsChannels = new();

        private readonly ConcurrentDictionary<long, Channel<AccountInfo>> _subscribedAccountInfoUpdateChannels = new();

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

        public long GetAccountId(long login) => _accountIds.GetValueOrDefault(login);

        public ProtoOACtidTraderAccount GetAccountById(long id) => _accounts.GetValueOrDefault(id);

        public ProtoOACtidTraderAccount GetAccountByLogin(long login) => _accounts.GetValueOrDefault(GetAccountId(login));

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

            //await FillConversionSymbols(model);

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

        public Channel<SymbolQuote> GetSymbolsQuoteChannel(long accountId)
        {
            if (_subscribedAccountQuoteChannels.TryAdd(accountId, Channel.CreateUnbounded<SymbolQuote>()))
            {
                return _subscribedAccountQuoteChannels[accountId];
            }
            else
            {
                throw new InvalidOperationException($"Couldn't add the quotes channel to {nameof(_subscribedAccountQuoteChannels)}");
            }
        }

        public void StopSymbolQuotes(long accountId)
        {
            if (!_subscribedAccountQuoteChannels.TryGetValue(accountId, out var channel)) return;

            _subscribedAccountQuoteChannels.TryRemove(accountId, out _);

            channel.Writer.TryComplete();
        }

        public Channel<Position> GetPositionUpdatesChannel(long accountId)
        {
            if (_subscribedAccountPositionUpdateChannels.TryAdd(accountId, Channel.CreateUnbounded<Position>()))
            {
                return _subscribedAccountPositionUpdateChannels[accountId];
            }
            else
            {
                throw new InvalidOperationException($"Couldn't add the positions channel to {nameof(_subscribedAccountPositionUpdateChannels)}");
            }
        }

        public void StopPositionUpdates(long accountId)
        {
            if (!_subscribedAccountPositionUpdateChannels.TryGetValue(accountId, out var channel)) return;

            _subscribedAccountPositionUpdateChannels.TryRemove(accountId, out _);

            channel.Writer.TryComplete();
        }

        public Channel<Error> GetErrorsChannel(long accountId)
        {
            if (_subscribedAccountErrorsChannels.TryAdd(accountId, Channel.CreateUnbounded<Error>()))
            {
                return _subscribedAccountErrorsChannels[accountId];
            }
            else
            {
                throw new InvalidOperationException($"Couldn't add the error channel to {nameof(_subscribedAccountErrorsChannels)}");
            }
        }

        public void StopErrors(long accountId)
        {
            if (!_subscribedAccountErrorsChannels.TryGetValue(accountId, out var channel)) return;

            _subscribedAccountErrorsChannels.TryRemove(accountId, out _);

            channel.Writer.TryComplete();
        }

        public async Task ClosePosition(long accountId, long positionId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var position = model.Positions.FirstOrDefault(iPosition => iPosition.Id == positionId);

            if (position is null) return;

            await _apiService.ClosePosition(positionId, position.Volume, accountId, model.IsLive);
        }

        public async Task CloseAllPosition(long accountId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var positions = model.Positions.ToArray();

            foreach (var position in positions)
            {
                await _apiService.ClosePosition(position.Id, position.Volume, accountId, model.IsLive);
            }
        }

        public Channel<PendingOrder> GetOrderUpdatesChannel(long accountId)
        {
            if (_subscribedAccountOrderUpdateChannels.TryAdd(accountId, Channel.CreateUnbounded<PendingOrder>()))
            {
                return _subscribedAccountOrderUpdateChannels[accountId];
            }
            else
            {
                throw new InvalidOperationException($"Couldn't add the order channel to {nameof(_subscribedAccountOrderUpdateChannels)}");
            }
        }

        public void StopOrderUpdates(long accountId)
        {
            if (!_subscribedAccountOrderUpdateChannels.TryGetValue(accountId, out var channel)) return;

            _subscribedAccountOrderUpdateChannels.TryRemove(accountId, out _);

            channel.Writer.TryComplete();
        }

        public async Task CancelOrder(long accountId, long orderId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var order = model.PendingOrders.FirstOrDefault(iOrder => iOrder.Id == orderId);

            if (order is null) return;

            await _apiService.CancelOrder(orderId, accountId, model.IsLive);
        }

        public async Task CancelAllOrders(long accountId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var orders = model.PendingOrders.ToArray();

            foreach (var order in orders)
            {
                await _apiService.CancelOrder(order.Id, accountId, model.IsLive);
            }
        }

        public Channel<AccountInfo> GetAccountInfoUpdatesChannel(long accountId)
        {
            if (_subscribedAccountInfoUpdateChannels.TryAdd(accountId, Channel.CreateUnbounded<AccountInfo>()))
            {
                return _subscribedAccountInfoUpdateChannels[accountId];
            }
            else
            {
                throw new InvalidOperationException($"Couldn't add the account info channel to {nameof(_subscribedAccountInfoUpdateChannels)}");
            }
        }

        public void StopAccountInfoUpdates(long accountId)
        {
            if (!_subscribedAccountInfoUpdateChannels.TryGetValue(accountId, out var channel)) return;

            _subscribedAccountInfoUpdateChannels.TryRemove(accountId, out _);

            channel.Writer.TryComplete();
        }

        public AccountInfo GetAccountInfo(long accountId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return null;

            return AccountInfo.FromModel(model);
        }

        public async Task CreateNewMarketOrder(NewMarketOrderRequest orderRequest)
        {
            var accountId = GetAccountId(orderRequest.AccountLogin);

            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var symbol = model.Symbols.FirstOrDefault(symbol => symbol.Id == orderRequest.SymbolId);

            if (symbol is null || Enum.TryParse<ProtoOATradeSide>(orderRequest.Direction, true, out var tradeSide) == false) return;

            await _apiService.CreateNewOrder(new MarketOrderModel
            {
                Symbol = symbol,
                Volume = MonetaryConverter.ToMonetary(orderRequest.Volume),
                TradeSide = tradeSide,
                Comment = orderRequest.Comment,
                IsMarketRange = orderRequest.IsMarketRange,
                MarketRangeInPips = orderRequest.MarketRange,
                BaseSlippagePrice = symbol.Bid,
                IsStopLossEnabled = orderRequest.HasStopLoss,
                StopLossInPips = orderRequest.StopLoss,
                IsTrailingStopLossEnabled = orderRequest.HasTrailingStop,
                IsTakeProfitEnabled = orderRequest.HasTakeProfit,
                TakeProfitInPips = orderRequest.TakeProfit
            }, accountId, model.IsLive);
        }

        public async Task ModifyMarketOrder(ModifyMarketOrderRequest orderRequest)
        {
            var accountId = GetAccountId(orderRequest.AccountLogin);

            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var order = model.Positions.FirstOrDefault(position => position.Id == orderRequest.Id);

            if (order is null || Enum.TryParse<ProtoOATradeSide>(orderRequest.Direction, true, out var tradeSide) == false) return;

            var newOrder = order.Clone();

            newOrder.Volume = MonetaryConverter.ToMonetary(orderRequest.Volume);
            newOrder.TradeSide = tradeSide;
            newOrder.IsStopLossEnabled = orderRequest.HasStopLoss;
            newOrder.StopLossInPips = orderRequest.StopLoss;
            newOrder.IsTrailingStopLossEnabled = orderRequest.HasTrailingStop;
            newOrder.IsTakeProfitEnabled = orderRequest.HasTakeProfit;
            newOrder.TakeProfitInPips = orderRequest.TakeProfit;

            await _apiService.ModifyPosition(order, newOrder, accountId, model.IsLive);
        }

        public async Task CreateNewPendingOrder(NewPendingOrderRequest orderRequest)
        {
            var accountId = GetAccountId(orderRequest.AccountLogin);

            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var symbol = model.Symbols.FirstOrDefault(symbol => symbol.Id == orderRequest.SymbolId);

            if (symbol is null || Enum.TryParse<PendingOrderType>(orderRequest.Type, true, out var type) == false || Enum.TryParse<ProtoOATradeSide>(orderRequest.Direction, true, out var tradeSide) == false) return;

            await _apiService.CreateNewOrder(new PendingOrderModel
            {
                Symbol = symbol,
                Volume = MonetaryConverter.ToMonetary(orderRequest.Volume),
                Price = orderRequest.Price,
                Type = type,
                TradeSide = tradeSide,
                Comment = orderRequest.Comment,
                LimitRangeInPips = orderRequest.LimitRange,
                IsExpiryEnabled = orderRequest.HasExpiry,
                ExpiryTime = orderRequest.Expiry,
                IsStopLossEnabled = orderRequest.HasStopLoss,
                StopLossInPips = orderRequest.StopLoss,
                IsTrailingStopLossEnabled = orderRequest.HasTrailingStop,
                IsTakeProfitEnabled = orderRequest.HasTakeProfit,
                TakeProfitInPips = orderRequest.TakeProfit
            }, accountId, model.IsLive);
        }

        public async Task ModifyPendingOrder(ModifyPendingOrderRequest orderRequest)
        {
            var accountId = GetAccountId(orderRequest.AccountLogin);

            if (_accountModels.TryGetValue(accountId, out var model) == false) return;

            var order = model.PendingOrders.FirstOrDefault(order => order.Id == orderRequest.Id);

            if (order is null) return;

            var newOrder = order.Clone();

            newOrder.Volume = MonetaryConverter.ToMonetary(orderRequest.Volume);
            newOrder.IsStopLossEnabled = orderRequest.HasStopLoss;
            newOrder.StopLossInPips = orderRequest.StopLoss;
            newOrder.IsTrailingStopLossEnabled = orderRequest.HasTrailingStop;
            newOrder.IsTakeProfitEnabled = orderRequest.HasTakeProfit;
            newOrder.TakeProfitInPips = orderRequest.TakeProfit;
            newOrder.Price = orderRequest.Price;
            newOrder.LimitRangeInPips = orderRequest.LimitRange;
            newOrder.IsExpiryEnabled = orderRequest.HasExpiry;
            newOrder.ExpiryTime = orderRequest.Expiry;

            await _apiService.ModifyOrder(order, newOrder, accountId, model.IsLive);
        }

        public async Task<IEnumerable<HistoricalTrade>> GetHistory(DateTimeOffset from, DateTimeOffset to, long accountId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return null;

            var trades = await _apiService.GetHistoricalTrades(accountId, model.IsLive, from, to);

            foreach (var trade in trades)
            {
                var tradeSymbol = model.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == trade.SymbolId);

                if (tradeSymbol is null) continue;

                trade.SymbolName = tradeSymbol.Name;
                trade.Volume = MonetaryConverter.FromMonetary(trade.Volume);
                trade.FilledVolume = MonetaryConverter.FromMonetary(trade.FilledVolume);
                trade.ClosedVolume = MonetaryConverter.FromMonetary(trade.ClosedVolume);
            }

            return trades;
        }

        public async Task<IEnumerable<Transaction>> GetTransactions(DateTimeOffset from, DateTimeOffset to, long accountId)
        {
            if (_accountModels.TryGetValue(accountId, out var model) == false) return null;

            var transactions = await _apiService.GetTransactions(accountId, model.IsLive, from, to);

            foreach (var transaction in transactions)
            {
                transaction.Delta = MonetaryConverter.FromMonetary(transaction.Delta);
                transaction.Balance = MonetaryConverter.FromMonetary(transaction.Balance);
            }

            return transactions;
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

        private async void OnSpotEvent(ProtoOASpotEvent spotEvent)
        {
            if (_accountModels.TryGetValue(spotEvent.CtidTraderAccountId, out var model) == false) return;

            var symbol = model.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == spotEvent.SymbolId);

            if (symbol is null) return;

            double bid = symbol.Bid;
            double ask = symbol.Ask;

            if (spotEvent.HasBid) bid = symbol.Data.GetPriceFromRelative((long)spotEvent.Bid);
            if (spotEvent.HasAsk) ask = symbol.Data.GetPriceFromRelative((long)spotEvent.Ask);

            var quote = new SymbolQuote(symbol.Id, bid, ask);

            symbol.OnTick(quote);

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
                var symbolPositions = model.Positions.Where(iPosition => iPosition.Symbol.Id == symbol.Id).ToArray();

                var isSubscribedForPositionUpdates = _subscribedAccountPositionUpdateChannels.TryGetValue(spotEvent.CtidTraderAccountId, out var positionUpdatesChannel);

                foreach (var position in symbolPositions)
                {
                    position.OnSymbolTick();

                    if (isSubscribedForPositionUpdates) await positionUpdatesChannel.Writer.WriteAsync(Position.FromModel(position));
                }

                model.UpdateStatus();

                if ((symbolPositions.Length > 0 || model.DepositAsset == symbol.BaseAsset || model.DepositAsset == symbol.QuoteAsset) && _subscribedAccountInfoUpdateChannels.TryGetValue(spotEvent.CtidTraderAccountId, out var accountInfoUpdateChannel))
                {
                    await accountInfoUpdateChannel.Writer.WriteAsync(AccountInfo.FromModel(model));
                }
            }

            if (_subscribedAccountQuoteChannels.TryGetValue(spotEvent.CtidTraderAccountId, out var quotesChannel))
            {
                await quotesChannel.Writer.WriteAsync(quote);
            }
        }

        private async void OnExecutionEvent(ProtoOAExecutionEvent executionEvent)
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
                        position = new MarketOrderModel(executionEvent.Position, symbol);

                        model.Positions.Add(position);
                    }

                    if (order is not null)
                    {
                        model.PendingOrders.Remove(order);

                        order.IsFilledOrCanceled = true;
                    }

                    break;

                case ProtoOAExecutionType.OrderCancelled:
                    if (order is not null)
                    {
                        model.PendingOrders.Remove(order);

                        order.IsFilledOrCanceled = true;
                    }
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
                    order = new PendingOrderModel(executionEvent.Order, symbol);
                    model.PendingOrders.Add(order);

                    break;

                case ProtoOAExecutionType.OrderReplaced:
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    if (order is not null) order.Update(executionEvent.Order, symbol);
                    break;

                case ProtoOAExecutionType.Swap:
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    break;
            }

            if (position is not null && _subscribedAccountPositionUpdateChannels.TryGetValue(executionEvent.CtidTraderAccountId, out var positionUpdateChannel))
            {
                await positionUpdateChannel.Writer.WriteAsync(Position.FromModel(position));
            }

            if (order is not null && _subscribedAccountOrderUpdateChannels.TryGetValue(executionEvent.CtidTraderAccountId, out var orderUpdateChannel))
            {
                await orderUpdateChannel.Writer.WriteAsync(PendingOrder.FromModel(order));
            }
        }

        private async Task AuthorizeAccounts(IEnumerable<ProtoOACtidTraderAccount> accounts, string accessToken)
        {
            foreach (var account in accounts)
            {
                var accountId = Convert.ToInt64(account.CtidTraderAccountId);

                await _apiService.AuthorizeAccount(accountId, account.IsLive, accessToken);

                _accounts.TryAdd(accountId, account);
                _accountIds.TryAdd(account.TraderLogin, accountId);
            }
        }

        private async void OnOrderErrorRes(ProtoOAOrderErrorEvent error)
        {
            if (!_subscribedAccountErrorsChannels.TryGetValue(error.CtidTraderAccountId, out var channel)) return;

            await channel.Writer.WriteAsync(new($"Code: {error.ErrorCode} | Description: {error.Description}", nameof(ProtoOAOrderErrorEvent)));
        }

        private async void OnOaErrorRes(ProtoOAErrorRes error)
        {
            if (!_subscribedAccountErrorsChannels.TryGetValue(error.CtidTraderAccountId, out var channel)) return;

            await channel.Writer.WriteAsync(new($"Code: {error.ErrorCode} | Description: {error.Description}", nameof(ProtoOAErrorRes)));
        }

        private async void OnErrorRes(ProtoErrorRes error)
        {
            var errorChannels = _subscribedAccountErrorsChannels.Values.ToArray();

            foreach (var channel in errorChannels)
            {
                await channel.Writer.WriteAsync(new($"Code: {error.ErrorCode} | Description: {error.Description}", nameof(ProtoErrorRes)));
            }
        }

        private void Subscribe(IObservable<IMessage> observable)
        {
            observable.OfType<ProtoOASpotEvent>().Subscribe(OnSpotEvent);
            observable.OfType<ProtoOAExecutionEvent>().Subscribe(OnExecutionEvent);
            observable.OfType<ProtoErrorRes>().Subscribe(OnErrorRes);
            observable.OfType<ProtoOAErrorRes>().Subscribe(OnOaErrorRes);
            observable.OfType<ProtoOAOrderErrorEvent>().Subscribe(OnOrderErrorRes);
        }
    }
}