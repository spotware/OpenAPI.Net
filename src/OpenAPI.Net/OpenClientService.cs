using Google.Protobuf;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Exceptions;
using OpenAPI.Net.Helpers;
using OpenAPI.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProtoOA.Request;
using ProtoOA.Enums;
using ProtoOA.Response;
using ProtoOA.CommonMessages;
using ProtoOA.Event;
using ProtoOA.Model;

namespace OpenAPI.Net
{
    public sealed partial class OpenClient
    {
        /// <summary>
        /// Maximum response waiting time in milliseconds, after that time
        /// throw TimeoutException.
        /// </summary>
        public static int MaximumResponseWaitTime = 30000;
        /// <summary>
        /// lastTimeStamp for NewMessageUniqueID generator
        /// lastTimeStamp is expressed in milliseconds
        /// </summary>
        private static long lastTimeStamp = DateTime.UtcNow.Ticks / 10000;
        private static long NewMessageUniqueID
        {
            get
            {
                long original, newValue;
                do
                {
                    original = lastTimeStamp;
                    long now = (DateTime.UtcNow.Ticks / 10000);
                    newValue = Math.Max(now, original + 1);
                } while (Interlocked.CompareExchange
                             (ref lastTimeStamp, newValue, original) != original);

                return newValue;
            }
        }
        private ConcurrentDictionary<long, ResultPointer> resultPointers = new ConcurrentDictionary<long, ResultPointer>();

        private async Task<IOAMessage> SendMessageWaitResponse(IOAMessage message, uint retryCount = 0)
        {
            long id = NewMessageUniqueID;
            string clientMsgId = id.ToString("X");
            IOAMessage resultMessage = null;
            ResultPointer rp = new ResultPointer();
            bool messageReceived = false;
            resultPointers.TryAdd(id, rp);
            for (uint retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    await SendMessage(message, message.PayloadType, clientMsgId);
                    messageReceived = rp.WaitHandle.WaitOne(MaximumResponseWaitTime);
                }
                catch
                {
                    resultPointers.TryRemove(id, out _);
                    rp.Dispose();
                    throw;
                }
                finally
                {
                    resultMessage = rp?.Message;
                    if (!messageReceived & retry == retryCount)
                    {
                        // timeout occured - Remove current ResultPointer
                        resultPointers.TryRemove(id, out _);
                        rp?.Dispose();
                    }
                }
                switch ((IMessage)resultMessage)
                {
                    case ProtoErrorRes protoErrorRes:
                        throw new ProtoErrorResException(protoErrorRes);
                    case ErrorRes protoOAErrorRes:
                        throw new ErrorResException(protoOAErrorRes);
                    case OrderErrorEvent protoOAOrderErrorEvent:
                        throw new OrderErrorEventException(protoOAOrderErrorEvent);
                    case null:
                        if (retry < retryCount)
                           continue;
                        throw new TimeoutException("Maximum response timeout reached.");
                    default:
                        return resultMessage;
                }
            }
            return resultMessage;
        }
        private void MessageForwardByClientMsgId(IMessage message, string clientMsgId)
        {
            if (long.TryParse(clientMsgId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long id))
            {
                if (resultPointers.TryRemove(id, out ResultPointer rp))
                {
                    rp.Message = (IOAMessage)message;
                    rp.WaitHandle.Set();
                }
                //else
                //{
                //    //Some requests have 2 responses, only 1-st  is handled
                //    // second goes here
                //}
            }
        }
        /// <summary>
        /// Request to refresh the access token using refresh token of granted trader's account.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<Token> RefreshToken(Token token)
        {
            var request = new RefreshTokenReq()
            {
                RefreshToken = token.RefreshToken,
                PayloadType = PayloadType.ProtoOaRefreshTokenReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            RefreshTokenRes res = (RefreshTokenRes)message;
            token.AccessToken = res.AccessToken;
            return token;
        }

        /// <summary>
        /// Request for a list of symbols available for a trading account.
        /// Symbol entries are returned with the limited set of fields.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="includeArchivedSymbols"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<SymbolsListRes> GetSymbolList(long accountId, bool includeArchivedSymbols = false)
        {
            var request = new SymbolsListReq
            {
                CtidTraderAccountId = accountId,
                IncludeArchivedSymbols = includeArchivedSymbols,
                PayloadType = PayloadType.ProtoOaSymbolsListReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            SymbolsListRes res = (SymbolsListRes)message;
            return new SymbolsListRes(res);
        }
        /// <summary>
        /// Request for getting Trader's current open positions and pending orders data.
        /// Same as GetOpenPositionsOrders
        /// </summary>
        /// <param name="accountId"></param>
        public async Task<ReconcileRes> GetReconcile(long accountId)
        {
            return await GetOpenPositionsOrders(accountId);
        }
        /// <summary>
        /// Request for getting Trader's current open positions and pending orders data.
        /// Same as GetReconcile
        /// </summary>
        /// <param name="accountId"></param>
        public async Task<ReconcileRes> GetOpenPositionsOrders(long accountId)
        {
            var request = new ReconcileReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaReconcileReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ReconcileRes res = (ReconcileRes)message;
            return new ReconcileRes(res);
        }
        /// <summary>
        /// Request for getting the list of granted trader's account for the access token.
        /// </summary>
        /// <param name="token">AccessToken</param>
        public async Task<CtidTraderAccount[]> GetAccountList(Token token)
        {
            var request = new GetAccountListByAccessTokenReq
            {
                AccessToken = token.AccessToken,
                PayloadType = PayloadType.ProtoOaGetAccountsByAccessTokenReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            GetAccountListByAccessTokenRes res = (GetAccountListByAccessTokenRes)message;
            return res.CtidTraderAccount.Select(o => new CtidTraderAccount(o)).ToArray();
        }

        /// <summary>
        /// Request for the authorizing Application.
        /// </summary>
        /// <param name="app"></param>
        public async Task<ApplicationAuthRes> ApplicationAuthorize(App app)
        {
            return await ApplicationAuthorize(app.ClientId, app.Secret);
        }
        /// <summary>
        /// Request for the authorizing Application.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="secret"></param>
        public async Task<ApplicationAuthRes> ApplicationAuthorize(string clientId, string secret)
        {
            var request = new ApplicationAuthReq
            {
                ClientId = clientId,
                ClientSecret = secret,
                PayloadType = PayloadType.ProtoOaApplicationAuthReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ApplicationAuthRes res = (ApplicationAuthRes)message;
            return new ApplicationAuthRes(res);
        }
        /// <summary>
        /// Request for the authorizing trading account session.
        /// Requires established authorized connection with the client application
        ///  using ApplicationAuthReq.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="token"></param>
        public async Task<AccountAuthRes> AccountAuthorize(long accountId, Token token)
        {
            var request = new AccountAuthReq
            {
                CtidTraderAccountId = accountId,
                AccessToken = token.AccessToken,
                PayloadType = PayloadType.ProtoOaAccountAuthReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            AccountAuthRes res = (AccountAuthRes)message;
            return new AccountAuthRes(res);
        }
        /// <summary>
        /// Request for logout of trading account session.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<AccountLogoutRes> AccountLogout(long accountId)
        {
            var request = new AccountLogoutReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaAccountLogoutReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            AccountLogoutRes res = (AccountLogoutRes)message;
            return new AccountLogoutRes(res);
        }

        public async Task<GetDynamicLeverageByIDRes> GetDynamicLeverageByID(long accountId, long leverageId)
        {
            var request = new GetDynamicLeverageByIDReq
            {
                CtidTraderAccountId = accountId,
                LeverageId = leverageId,
                PayloadType = PayloadType.ProtoOaGetDynamicLeverageReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            GetDynamicLeverageByIDRes res = (GetDynamicLeverageByIDRes)message;
            return new GetDynamicLeverageByIDRes(res);
        }
        /// <summary>
        /// Request for getting historical tick data for the symbol.
        /// </summary>
        /// <param name="accountId">Unique identifier of the trader's account. Used to match responses to trader's accounts.</param>
        /// <param name="SymbolId">Unique identifier of the Symbol in cTrader platform.</param>
        /// <param name="FromDateTime">The exact UTC time of starting the search</param>
        /// <param name="ToDateTime">The exact UTC time of finishing the search</param>
        /// <param name="Type">Bid or Ask</param>
        public async Task<TickData[]> GetTickData(long accountId, long SymbolId,
            DateTimeOffset FromDateTime, DateTimeOffset ToDateTime, QuoteType Type)
        {
            List<TickData> tickData = new();
            GetTickDataRes res;
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long fromTime2;
            do
            {
                long fromMin = toTime - 604800000;
                fromTime2 = Math.Max(fromTime, fromMin);
                var request = new GetTickDataReq
                {
                    CtidTraderAccountId = accountId,
                    SymbolId = SymbolId,
                    FromTimestamp = fromTime2,
                    ToTimestamp = toTime,
                    Type = (QuoteType)Type,
                    PayloadType = PayloadType.ProtoOaGetTickdataReq
                };

                IOAMessage message = await SendMessageWaitResponse(request, 2);
                res = (GetTickDataRes)message;
                var chunkData = res.TickData.ToList();
                if (chunkData.Count > 1)
                {
                    for (int i = 1; i < chunkData.Count; i++)
                    {
                        chunkData[i].Tick += chunkData[i - 1].Tick;
                        chunkData[i].Timestamp += chunkData[i - 1].Timestamp;
                    }
                }
                tickData.AddRange(chunkData);
                if (chunkData.Count > 0)
                {
                    toTime = chunkData.Last().Timestamp - 1;
                }
                else
                {
                    toTime = toTime - 604800000;
                    if (toTime <= fromTime)
                        break;
                }
            } while (res.HasMore | fromTime2 > fromTime);
            return tickData.Select(o => new TickData(o)).ToArray();
        }

        /// <summary>
        /// Request for getting a full symbol entity.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="SymbolIds"></param>
        public async Task<SymbolByIdRes> GetSymbolsFull(long accountId, IEnumerable<long> SymbolIds)
        {
            var request = new SymbolByIdReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaSymbolByIdReq
            };
            request.SymbolId.AddRange(SymbolIds);

            IOAMessage message = await SendMessageWaitResponse(request);
            SymbolByIdRes res = (SymbolByIdRes)message;
            return new SymbolByIdRes(res);
        }

        /// <summary>
        /// Request for getting details of Trader's profile.
        /// Limited due to GDRP requirements.
        /// </summary>
        /// <param name="token">AccessToken</param>
        /// <returns></returns>
        public async Task<CtidProfile> GetProfile(Token token)
        {
            var request = new GetCtidProfileByTokenReq
            {
                AccessToken = token.AccessToken,
                PayloadType = PayloadType.ProtoOaGetCtidProfileByTokenReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            GetCtidProfileByTokenRes res = (GetCtidProfileByTokenRes)message;
            return new CtidProfile(res.Profile);
        }
        /// <summary>
        /// Request for getting a conversion chain between two assets that consists of several symbols.
        /// Use when no direct quote is available
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="baseAssetId"></param>
        /// <param name="quoteAssetId"></param>
        /// <returns></returns>
        public async Task<LightSymbol[]> GetSymbolsForConversion(long accountId, long baseAssetId, long quoteAssetId)
        {
            var request = new SymbolsForConversionReq
            {
                CtidTraderAccountId = accountId,
                FirstAssetId = baseAssetId,
                LastAssetId = quoteAssetId,
                PayloadType = PayloadType.ProtoOaSymbolsForConversionReq
            };
            IOAMessage message = await SendMessageWaitResponse(request);
            SymbolsForConversionRes res = (SymbolsForConversionRes)message;
            return res.Symbol.Select(o => new LightSymbol(o)).ToArray();
        }
        /// <summary>
        /// Request for sending a new trading order.
        /// Allowed only if the accessToken has the "trade" permissions for the trading account.
        /// </summary>
        /// <param name="orderModel"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ExecutionEvent> CreateNewOrder(OrderModel orderModel, long accountId)
        {
            var requestMessage = new NewOrderReq
            {
                CtidTraderAccountId = accountId,
                SymbolId = orderModel.Symbol.Id,
                Volume = orderModel.Volume,
                TradeSide = orderModel.TradeSide,
                PayloadType = PayloadType.ProtoOaNewOrderReq
            };
            if (orderModel is MarketOrderModel marketOrder)
            {
                if (marketOrder.IsMarketRange)
                {
                    requestMessage.OrderType = OrderType.MarketRange;
                    requestMessage.BaseSlippagePrice = marketOrder.BaseSlippagePrice;
                    var slippageinPoint = orderModel.Symbol.Data.GetPointsFromPips(marketOrder.MarketRangeInPips);
                    if (slippageinPoint < int.MaxValue) requestMessage.SlippageInPoints = (int)slippageinPoint;
                }
                else
                {
                    requestMessage.OrderType = OrderType.Market;
                }
                if (marketOrder.Id != default)
                {
                    requestMessage.PositionId = marketOrder.Id;
                }
            }
            else if (orderModel is PendingOrderModel pendingOrder)
            {
                requestMessage.OrderType = pendingOrder.ProtoType;
                if (requestMessage.OrderType == OrderType.Limit)
                {
                    requestMessage.LimitPrice = pendingOrder.Price;
                }
                else
                {
                    requestMessage.StopPrice = pendingOrder.Price;
                    if (requestMessage.OrderType == OrderType.StopLimit)
                    {
                        var slippageinPoint = pendingOrder.Symbol.Data.GetPointsFromPips(pendingOrder.LimitRangeInPips);
                        if (slippageinPoint < int.MaxValue) requestMessage.SlippageInPoints = (int)slippageinPoint;
                    }
                }
                if (pendingOrder.IsExpiryEnabled)
                {
                    requestMessage.ExpirationTimestamp = pendingOrder.ExpiryTime.ToUnixTimeMilliseconds();
                }
            }
            if (orderModel.IsStopLossEnabled)
            {
                requestMessage.TrailingStopLoss = orderModel.IsTrailingStopLossEnabled;
                requestMessage.RelativeStopLoss = orderModel.RelativeStopLoss;
            }
            if (orderModel.IsTakeProfitEnabled)
            {
                requestMessage.RelativeTakeProfit = orderModel.RelativeTakeProfit;
            }
            if (string.IsNullOrWhiteSpace(orderModel.Label) is not true)
            {
                requestMessage.Label = orderModel.Label;
            }
            if (string.IsNullOrWhiteSpace(orderModel.Comment) is not true)
            {
                requestMessage.Comment = orderModel.Comment;
            }
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ExecutionEvent res = (ExecutionEvent)message;
            return new ExecutionEvent(res);
        }
        /// <summary>
        /// Request for closing or partially closing of an existing position.
        /// Allowed only if the accessToken has "trade" permissions for the trading account.
        /// </summary>
        /// <param name="positionId"></param>
        /// <param name="volume"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ExecutionEvent> ClosePosition(long positionId, long volume, long accountId)
        {
            var requestMessage = new ClosePositionReq
            {
                CtidTraderAccountId = accountId,
                PositionId = positionId,
                Volume = volume,
                PayloadType = PayloadType.ProtoOaClosePositionReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ExecutionEvent res = (ExecutionEvent)message;
            return new ExecutionEvent(res);
        }
        /// <summary>
        /// Request for cancelling existing pending order.
        /// Allowed only if the accessToken has "trade" permissions for the trading account.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ExecutionEvent> CancelOrder(long orderId, long accountId)
        {
            var requestMessage = new CancelOrderReq
            {
                CtidTraderAccountId = accountId,
                OrderId = orderId,
                PayloadType = PayloadType.ProtoOaCancelOrderReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ExecutionEvent res = (ExecutionEvent)message;
            return new ExecutionEvent(res);
        }
        /// <summary>
        /// Request for getting data of Trader's Account.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<Trader> GetTrader(long accountId)
        {
            var requestMessage = new TraderReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaTraderReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            TraderRes res = (TraderRes)message;
            return new Trader(res.Trader);
        }
        /// <summary>
        /// Request for modifying the existing pending order.
        /// Allowed only if the Access Token has "trade" permissions for the trading account.
        /// </summary>
        /// <param name="oldOrder"></param>
        /// <param name="newOrder"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ExecutionEvent> ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long accountId)
        {
            var requestMessage = new AmendOrderReq
            {
                OrderId = oldOrder.Id,
                CtidTraderAccountId = accountId,
                Volume = newOrder.Volume,
                PayloadType = PayloadType.ProtoOaAmendOrderReq
            };
            if (newOrder.IsStopLossEnabled)
            {
                if (newOrder.StopLossInPips != default && newOrder.StopLossInPrice == default)
                {
                    newOrder.StopLossInPrice = newOrder.TradeSide == TradeSide.Sell
                        ? newOrder.Symbol.Data.AddPipsToPrice(newOrder.Price, newOrder.StopLossInPips)
                        : newOrder.Symbol.Data.SubtractPipsFromPrice(newOrder.Price, newOrder.StopLossInPips);
                }
                requestMessage.StopLoss = newOrder.StopLossInPrice;
                requestMessage.TrailingStopLoss = newOrder.IsTrailingStopLossEnabled;
            }
            if (newOrder.IsTakeProfitEnabled)
            {
                if (newOrder.TakeProfitInPips != default && newOrder.TakeProfitInPrice == default)
                {
                    newOrder.TakeProfitInPrice = newOrder.TradeSide == TradeSide.Sell
                        ? newOrder.Symbol.Data.SubtractPipsFromPrice(newOrder.Price, newOrder.TakeProfitInPips)
                        : newOrder.Symbol.Data.AddPipsToPrice(newOrder.Price, newOrder.TakeProfitInPips);
                }
                requestMessage.TakeProfit = newOrder.TakeProfitInPrice;
            }
            if (newOrder.IsExpiryEnabled)
            {
                requestMessage.ExpirationTimestamp = newOrder.ExpiryTime.ToUnixTimeMilliseconds();
            }
            if (newOrder.Type == PendingOrderType.Limit)
            {
                requestMessage.LimitPrice = newOrder.Price;
            }
            else
            {
                requestMessage.StopPrice = newOrder.Price;
                if (newOrder.Type == PendingOrderType.StopLimit)
                {
                    var slippageinPoint = newOrder.Symbol.Data.GetPointsFromPips(newOrder.LimitRangeInPips);
                    if (slippageinPoint < int.MaxValue) requestMessage.SlippageInPoints = (int)slippageinPoint;
                }
            }
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ExecutionEvent res = (ExecutionEvent)message;
            return new ExecutionEvent(res);
        }
        /// <summary>
        /// Request for getting Trader's deals historical data (execution details).
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="FromDateTime"></param>
        /// <param name="ToDateTime"></param>
        /// <returns></returns>
        public async Task<DealListRes> GetHistoricalTrades(long accountId, DateTimeOffset FromDateTime, DateTimeOffset ToDateTime)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var requestMessage = new DealListReq
            {
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaDealListReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            DealListRes res = (DealListRes)message;
            //HistoricalTrade[] trades = res.Deal
            //    .Select(deal => new HistoricalTrade(deal))
            //    .ToArray();
            return new DealListRes(res);
        }
        /// <summary>
        /// Request for getting Trader's historical data of deposits and withdrawals.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="FromDateTime"></param>
        /// <param name="ToDateTime"></param>
        /// <returns></returns>
        public async Task<CashFlowHistoryListRes> GetTransactions(long accountId, DateTimeOffset FromDateTime, DateTimeOffset ToDateTime)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var requestMessage = new CashFlowHistoryListReq
            {
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaCashFlowHistoryListReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            CashFlowHistoryListRes res = (CashFlowHistoryListRes)message;
            //Transaction[] transactions = res.DepositWithdraw
            //    .Select(dw => new Transaction(dw))
            //    .ToArray();
            return new CashFlowHistoryListRes(res);
        }
        /// <summary>
        /// Request for subscribing on spot events of the specified symbol.
        /// You need to subscribe to OpenClient.SpotEvent or OpenClient.SpotObservable to get the data
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<SubscribeSpotsRes> SubscribeToSpots(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new SubscribeSpotsReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaSubscribeSpotsReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            SubscribeSpotsRes res = (SubscribeSpotsRes)message;
            return new SubscribeSpotsRes(res);
        }
        /// <summary>
        /// Request for unsubscribing from the spot events of the specified symbol.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<UnsubscribeSpotsRes> UnsubscribeFromSpots(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new UnsubscribeSpotsReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaUnsubscribeSpotsReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            UnsubscribeSpotsRes res = (UnsubscribeSpotsRes)message;
            return new UnsubscribeSpotsRes(res);
        }
        /// <summary>
        /// Request for subscribing for live trend bars.
        /// You need to subscribe to OpenClient.SpotEvent or OpenClient.SpotObservable to get the data
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public async Task<SubscribeLiveTrendbarRes> SubscribeToLiveTrendbar(long accountId, long symbolId, TrendbarPeriod period)
        {
            var requestMessage = new SubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = accountId,
                Period = (TrendbarPeriod)period,
                SymbolId = symbolId,
                PayloadType = PayloadType.ProtoOaSubscribeLiveTrendbarReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            SubscribeLiveTrendbarRes res = (SubscribeLiveTrendbarRes)message;
            return new SubscribeLiveTrendbarRes(res);
        }
        /// <summary>
        /// Request for unsubscribing from the live trend bars.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public async Task<UnsubscribeLiveTrendbarRes> UnsubscribeFromLiveTrendbar(long accountId, long symbolId, TrendbarPeriod period)
        {
            var requestMessage = new UnsubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = accountId,
                Period = (TrendbarPeriod)period,
                SymbolId = symbolId,
                PayloadType = PayloadType.ProtoOaUnsubscribeLiveTrendbarReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            UnsubscribeLiveTrendbarRes res = (UnsubscribeLiveTrendbarRes)message;
            return new UnsubscribeLiveTrendbarRes(res);
        }


        /// <summary>
        /// Request for subscribing on spot events of the specified symbol.
        /// TODO:EventHandler
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<SubscribeDepthQuotesRes> SubscribeToDepths(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new SubscribeDepthQuotesReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaSubscribeDepthQuotesReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            SubscribeDepthQuotesRes res = (SubscribeDepthQuotesRes)message;
            return new SubscribeDepthQuotesRes(res);
        }
        /// <summary>
        /// Request for unsubscribing from the depth of market of the specified symbol.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<UnsubscribeDepthQuotesRes> UnsubscribeFromDepths(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new UnsubscribeDepthQuotesReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaUnsubscribeDepthQuotesReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            UnsubscribeDepthQuotesRes res = (UnsubscribeDepthQuotesRes)message;
            return new UnsubscribeDepthQuotesRes(res);
        }


        /// <summary>
        /// Request for getting historical trend bars for the symbol.
        /// </summary>
        /// <param name="accountId">Unique identifier of the trader's account. Used to match responses to trader's accounts.</param>
        /// <param name="symbolId">Unique identifier of the Symbol in cTrader platform.</param>
        /// <param name="period">Specifies period of trend bar series (e.g. M1, M10, etc.).</param>
        /// <param name="from">The exact UTC time of starting the search.
        /// Validation: (ToDateTime - FromDateTime).ToUnixTimeMilliseconds() <= X, where X depends on series
        /// period: M1, M2, M3, M4, M5: 302400000 (5 weeks);
        /// M10, M15, M30, H1: 21168000000 (35 weeks);
        /// H4, H12, D1: 31622400000 (1 year);
        /// W1, MN1: 158112000000 (5 years).</param>
        /// <param name="to">The exact UTC time of finishing the search</param>
        /// <param name="count">Limit number of trend bars in response back from ToDateTime.</param>
        public async Task<GetTrendbarsRes> GetTrendbars(long accountId, long symbolId, TrendbarPeriod period, DateTimeOffset from = default, DateTimeOffset to = default, uint count = default)
        {
            var periodMaximum = period.GetMaximumTime();
            if(from == default & to == default)
            {
                to = DateTimeOffset.UtcNow.AddSeconds(1);
            }
            if (to == default) to = from.Add(periodMaximum);
            if (from == default) from = to.Add(-periodMaximum);
            if (to - from > periodMaximum | to < from) throw new ArgumentOutOfRangeException(nameof(to), "The time range is not valid");
            var requestMessage = new GetTrendbarsReq
            {
                FromTimestamp = from.ToUnixTimeMilliseconds(),
                ToTimestamp = to.ToUnixTimeMilliseconds(),
                CtidTraderAccountId = accountId,
                Period = (TrendbarPeriod)period,
                SymbolId = symbolId,
                PayloadType = PayloadType.ProtoOaGetTrendbarsReq
            };
            if(count != default) requestMessage.Count = count;
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            GetTrendbarsRes res = (GetTrendbarsRes)message;
            return new GetTrendbarsRes(res);
        }
        /// <summary>
        /// Request for the list of assets available for a trader's account.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<AssetListRes> GetAssets(long accountId)
        {
            var requestMessage = new AssetListReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = PayloadType.ProtoOaAssetListReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            AssetListRes res = (AssetListRes)message;
            return new AssetListRes(res);
        }
        /// <summary>
        /// Request for getting the proxy version.
        /// Can be used to check the current version of the Open API scheme.
        /// </summary>
        /// <returns></returns>
        public async Task<VersionRes> GetVersion()
        {
            var requestMessage = new VersionReq()
            {
                PayloadType = PayloadType.ProtoOaVersionReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            VersionRes res = (VersionRes)message;
            return new VersionRes(res);
        }
    }
}
