// Warning: Work in progress, Not tested

using Google.Protobuf;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Exceptions;
using OpenAPI.Net.Helpers;
using OpenAPI.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public sealed partial class OpenClient
    {
        /// <summary>
        /// Maximum response waiting time in milliseconds, after that time
        /// throw TimeoutException.
        /// </summary>
        public static int MaximumResponseWaitTime = 60000;
        /// <summary>
        /// lastTimeStamp for NewMessageUniqueID generator
        /// lastTimeStamp is expressed in 1/100th of seconds
        /// </summary>
        private static ulong lastTimeStamp = (ulong)(DateTime.UtcNow.Ticks / 100000);
        private static ulong NewMessageUniqueID
        {
            get
            {
                ulong original, newValue;
                do
                {
                    original = lastTimeStamp;
                    ulong now = 100 * (ulong)(DateTime.UtcNow.Ticks / 100000);
                    newValue = Math.Max(now, original + 1);
                } while (Interlocked.CompareExchange
                             (ref lastTimeStamp, newValue, original) != original);

                return newValue;
            }
        }
        private ConcurrentDictionary<ulong, ResultPointer> resultPointers = new ConcurrentDictionary<ulong, ResultPointer>();

        private async Task<IOAMessage> SendMessageWaitResponse(IOAMessage message)
        {
            ulong id = NewMessageUniqueID;
            string clientMsgId = id.ToString();
            IOAMessage resultMessage = null;
            ResultPointer rp = new ResultPointer();
            bool messageReceived = false;
            try
            {
                _ = resultPointers.TryAdd(id, rp);
                await SendMessage(message, message.PayloadType, clientMsgId);
                messageReceived = rp.WaitHandle.WaitOne(MaximumResponseWaitTime);
            }
            finally
            {
                if (!messageReceived)
                {
                    // timeout or exception occured - Remove current ResultPointer
                    resultPointers.TryRemove(id, out _);
                }
                resultMessage = rp.Message;
                rp.Dispose();
            }
            switch((IMessage)resultMessage)
            {
                case ProtoErrorRes protoErrorRes:
                    throw new ProtoErrorResException(protoErrorRes);
                case ProtoOAErrorRes protoOAErrorRes:
                    throw new ProtoOAErrorResException(protoOAErrorRes);
                case ProtoOAOrderErrorEvent protoOAOrderErrorEvent:
                    throw new ProtoOAOrderErrorEventException(protoOAOrderErrorEvent);
                case null:
                    throw new TimeoutException("Maximum response timeout reached.");
            }
            return resultMessage;
        }
        private void MessageForwardByClientMsgId(IMessage message, string clientMsgId)
        {
            if (ulong.TryParse(clientMsgId, out ulong id))
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
            var request = new ProtoOARefreshTokenReq()
            {
                RefreshToken = token.RefreshToken,
                PayloadType = ProtoOAPayloadType.ProtoOaRefreshTokenReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOARefreshTokenRes res = (ProtoOARefreshTokenRes)message;
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
        public async Task<ProtoOASymbolsListRes> GetSymbolList(long accountId, bool includeArchivedSymbols = false)
        {
            var request = new ProtoOASymbolsListReq
            {
                CtidTraderAccountId = accountId,
                IncludeArchivedSymbols = includeArchivedSymbols,
                PayloadType = ProtoOAPayloadType.ProtoOaSymbolsListReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOASymbolsListRes res = (ProtoOASymbolsListRes)message;
            return res;
        }

        /// <summary>
        /// Request for getting Trader's current open positions and pending orders data.
        /// </summary>
        /// <param name="accountId"></param>
        public async Task<ProtoOAReconcileRes> GetOpenPositionsOrders(long accountId)
        {
            var request = new ProtoOAReconcileReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaReconcileReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAReconcileRes res = (ProtoOAReconcileRes)message;
            return res;
        }
        /// <summary>
        /// Request for getting the list of granted trader's account for the access token.
        /// </summary>
        /// <param name="token">AccessToken</param>
        public async Task<ProtoOACtidTraderAccount[]> GetAccountList(Token token)
        {
            var request = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = token.AccessToken,
                PayloadType = ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAGetAccountListByAccessTokenRes res = (ProtoOAGetAccountListByAccessTokenRes)message;
            return res.CtidTraderAccount.ToArray();
        }
        /// <summary>
        /// Request for the authorizing Application.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="secret"></param>
        public async Task<ProtoOAApplicationAuthRes> ApplicationAuthorize(string clientId, string secret)
        {
            var request = new ProtoOAApplicationAuthReq
            {
                ClientId = clientId,
                ClientSecret = secret,
                PayloadType = ProtoOAPayloadType.ProtoOaApplicationAuthReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAApplicationAuthRes res = (ProtoOAApplicationAuthRes)message;
            return res;
        }
        /// <summary>
        /// Request for the authorizing trading account session.
        /// Requires established authorized connection with the client application
        ///  using ProtoOAApplicationAuthReq.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="token"></param>
        public async Task<ProtoOAAccountAuthRes> AccountAuthorize(long accountId, Token token)
        {
            var request = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = accountId,
                AccessToken = token.AccessToken,
                PayloadType = ProtoOAPayloadType.ProtoOaAccountAuthReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAAccountAuthRes res = (ProtoOAAccountAuthRes)message;
            return res;
        }
        /// <summary>
        /// Request for logout of trading account session.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOAAccountLogoutRes> AccountLogout(long accountId)
        {
            var request = new ProtoOAAccountLogoutReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaAccountLogoutReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAAccountLogoutRes res = (ProtoOAAccountLogoutRes)message;
            return res;
        }

        public async Task<ProtoOAGetDynamicLeverageByIDRes> GetDynamicLeverageByID(long accountId, long leverageId)
        {
            var request = new ProtoOAGetDynamicLeverageByIDReq
            {
                CtidTraderAccountId = accountId,
                LeverageId = leverageId,
                PayloadType = ProtoOAPayloadType.ProtoOaGetDynamicLeverageReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAGetDynamicLeverageByIDRes res = (ProtoOAGetDynamicLeverageByIDRes)message;
            return res;
        }
        /// <summary>
        /// Request for getting historical tick data for the symbol. Maximum is 1 week.
        /// </summary>
        /// <param name="accountId">Unique identifier of the trader's account. Used to match responses to trader's accounts.</param>
        /// <param name="SymbolId">Unique identifier of the Symbol in cTrader platform.</param>
        /// <param name="FromDateTime">The exact UTC time of starting the search</param>
        /// <param name="ToDateTime">The exact UTC time of finishing the search</param>
        /// <param name="Type">Bid or Ask</param>
        public async Task<ProtoOATickData[]> GetTickData(long accountId, long SymbolId,
            DateTimeOffset FromDateTime, DateTimeOffset ToDateTime, ProtoOAQuoteType Type)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var request = new ProtoOAGetTickDataReq
            {
                CtidTraderAccountId = accountId,
                SymbolId = SymbolId,
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                Type = Type,
                PayloadType = ProtoOAPayloadType.ProtoOaGetTickdataReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAGetTickDataRes res = (ProtoOAGetTickDataRes)message;
            var tickData = res.TickData.ToArray();
            for (int i = 1; i < tickData.Length; i++)
            {
                tickData[i].Tick += tickData[i - 1].Tick;
                tickData[i].Timestamp += tickData[i - 1].Timestamp;
            }
            return tickData;
        }

        /// <summary>
        /// Request for getting a full symbol entity.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="SymbolIds"></param>
        public async Task<ProtoOASymbolByIdRes> GetSymbolsFull(long accountId, IEnumerable<long> SymbolIds)
        {
            var request = new ProtoOASymbolByIdReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaSymbolByIdReq
            };
            request.SymbolId.AddRange(SymbolIds);

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOASymbolByIdRes res = (ProtoOASymbolByIdRes)message;
            return res;
        }

        /// <summary>
        /// Request for getting details of Trader's profile.
        /// Limited due to GDRP requirements.
        /// </summary>
        /// <param name="token">AccessToken</param>
        /// <returns></returns>
        public async Task<ProtoOACtidProfile> GetProfile(Token token)
        {
            var request = new ProtoOAGetCtidProfileByTokenReq
            {
                AccessToken = token.AccessToken,
                PayloadType = ProtoOAPayloadType.ProtoOaGetCtidProfileByTokenReq
            };

            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOAGetCtidProfileByTokenRes res = (ProtoOAGetCtidProfileByTokenRes)message;
            return res.Profile;
        }
        /// <summary>
        /// Request for getting a conversion chain between two assets that consists of several symbols.
        /// Use when no direct quote is available
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="baseAssetId"></param>
        /// <param name="quoteAssetId"></param>
        /// <returns></returns>
        public async Task<ProtoOALightSymbol[]> GetSymbolsForConversion(long accountId, long baseAssetId, long quoteAssetId)
        {
            var request = new ProtoOASymbolsForConversionReq
            {
                CtidTraderAccountId = accountId,
                FirstAssetId = baseAssetId,
                LastAssetId = quoteAssetId,
                PayloadType = ProtoOAPayloadType.ProtoOaSymbolsForConversionReq
            };
            IOAMessage message = await SendMessageWaitResponse(request);
            ProtoOASymbolsForConversionRes res = (ProtoOASymbolsForConversionRes)message;
            return res.Symbol.ToArray();
        }
        /// <summary>
        /// Request for sending a new trading order.
        /// Allowed only if the accessToken has the "trade" permissions for the trading account.
        /// </summary>
        /// <param name="orderModel"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOAExecutionEvent> CreateNewOrder(OrderModel orderModel, long accountId)
        {
            ulong id = NewMessageUniqueID;
            string clientMsgId = id.ToString();
            var requestMessage = new ProtoOANewOrderReq
            {
                CtidTraderAccountId = accountId,
                SymbolId = orderModel.Symbol.Id,
                Volume = orderModel.Volume,
                TradeSide = orderModel.TradeSide,
                PayloadType = ProtoOAPayloadType.ProtoOaNewOrderReq
            };
            if (orderModel is MarketOrderModel marketOrder)
            {
                if (marketOrder.IsMarketRange)
                {
                    requestMessage.OrderType = ProtoOAOrderType.MarketRange;
                    requestMessage.BaseSlippagePrice = marketOrder.BaseSlippagePrice;
                    var slippageinPoint = orderModel.Symbol.Data.GetPointsFromPips(marketOrder.MarketRangeInPips);
                    if (slippageinPoint < int.MaxValue) requestMessage.SlippageInPoints = (int)slippageinPoint;
                }
                else
                {
                    requestMessage.OrderType = ProtoOAOrderType.Market;
                }
                if (marketOrder.Id != default)
                {
                    requestMessage.PositionId = marketOrder.Id;
                }
            }
            else if (orderModel is PendingOrderModel pendingOrder)
            {
                requestMessage.OrderType = pendingOrder.ProtoType;
                if (requestMessage.OrderType == ProtoOAOrderType.Limit)
                {
                    requestMessage.LimitPrice = pendingOrder.Price;
                }
                else
                {
                    requestMessage.StopPrice = pendingOrder.Price;
                    if (requestMessage.OrderType == ProtoOAOrderType.StopLimit)
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
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        /// <summary>
        /// Request for closing or partially closing of an existing position.
        /// Allowed only if the accessToken has "trade" permissions for the trading account.
        /// </summary>
        /// <param name="positionId"></param>
        /// <param name="volume"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOAExecutionEvent> ClosePosition(long positionId, long volume, long accountId)
        {
            var requestMessage = new ProtoOAClosePositionReq
            {
                CtidTraderAccountId = accountId,
                PositionId = positionId,
                Volume = volume,
                PayloadType = ProtoOAPayloadType.ProtoOaClosePositionReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        /// <summary>
        /// Request for cancelling existing pending order.
        /// Allowed only if the accessToken has "trade" permissions for the trading account.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOAExecutionEvent> CancelOrder(long orderId, long accountId)
        {
            var requestMessage = new ProtoOACancelOrderReq
            {
                CtidTraderAccountId = accountId,
                OrderId = orderId,
                PayloadType = ProtoOAPayloadType.ProtoOaCancelOrderReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        /// <summary>
        /// Request for getting data of Trader's Account.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOATrader> GetTrader(long accountId)
        {
            var requestMessage = new ProtoOATraderReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaTraderReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOATraderRes res = (ProtoOATraderRes)message;
            return res.Trader;
        }
        /// <summary>
        /// Request for modifying the existing pending order.
        /// Allowed only if the Access Token has "trade" permissions for the trading account.
        /// </summary>
        /// <param name="oldOrder"></param>
        /// <param name="newOrder"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOAExecutionEvent> ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long accountId)
        {
            var requestMessage = new ProtoOAAmendOrderReq
            {
                OrderId = oldOrder.Id,
                CtidTraderAccountId = accountId,
                Volume = newOrder.Volume,
                PayloadType = ProtoOAPayloadType.ProtoOaAmendOrderReq
            };
            if (newOrder.IsStopLossEnabled)
            {
                if (newOrder.StopLossInPips != default && newOrder.StopLossInPrice == default)
                {
                    newOrder.StopLossInPrice = newOrder.TradeSide == ProtoOATradeSide.Sell
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
                    newOrder.TakeProfitInPrice = newOrder.TradeSide == ProtoOATradeSide.Sell
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
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        /// <summary>
        /// Request for getting Trader's deals historical data (execution details).
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="FromDateTime"></param>
        /// <param name="ToDateTime"></param>
        /// <returns></returns>
        public async Task<HistoricalTrade[]> GetHistoricalTrades(long accountId, DateTimeOffset FromDateTime, DateTimeOffset ToDateTime)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var requestMessage = new ProtoOADealListReq
            {
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaDealListReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOADealListRes res = (ProtoOADealListRes)message;
            HistoricalTrade[] trades = res.Deal
                .Select(deal => new HistoricalTrade(deal))
                .ToArray();
            return trades;
        }
        /// <summary>
        /// Request for getting Trader's historical data of deposits and withdrawals.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="FromDateTime"></param>
        /// <param name="ToDateTime"></param>
        /// <returns></returns>
        public async Task<Transaction[]> GetTransactions(long accountId, DateTimeOffset FromDateTime, DateTimeOffset ToDateTime)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var requestMessage = new ProtoOACashFlowHistoryListReq
            {
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaCashFlowHistoryListReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOACashFlowHistoryListRes res = (ProtoOACashFlowHistoryListRes)message;
            Transaction[] transactions = res.DepositWithdraw
                .Select(dw => new Transaction(dw))
                .ToArray();
            return transactions;
        }
        /// <summary>
        /// Request for subscribing on spot events of the specified symbol.
        /// TODO:EventHandler
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<ProtoOASubscribeSpotsRes> SubscribeToSpots(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new ProtoOASubscribeSpotsReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaSubscribeSpotsReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOASubscribeSpotsRes res = (ProtoOASubscribeSpotsRes)message;
            return res;
        }
        /// <summary>
        /// Request for unsubscribing from the spot events of the specified symbol.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<ProtoOAUnsubscribeSpotsRes> UnsubscribeFromSpots(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new ProtoOAUnsubscribeSpotsReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaUnsubscribeSpotsReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAUnsubscribeSpotsRes res = (ProtoOAUnsubscribeSpotsRes)message;
            return res;
        }
        /// <summary>
        /// Request for subscribing for live trend bars.
        /// TODO:EventHandler
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public async Task<ProtoOASubscribeLiveTrendbarRes> SubscribeToLiveTrendbar(long accountId, long symbolId, ProtoOATrendbarPeriod period)
        {
            var requestMessage = new ProtoOASubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = accountId,
                Period = period,
                SymbolId = symbolId,
                PayloadType = ProtoOAPayloadType.ProtoOaSubscribeLiveTrendbarReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOASubscribeLiveTrendbarRes res = (ProtoOASubscribeLiveTrendbarRes)message;
            return res;
        }
        /// <summary>
        /// Request for unsubscribing from the live trend bars.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public async Task<ProtoOAUnsubscribeLiveTrendbarRes> UnsubscribeFromLiveTrendbar(long accountId, long symbolId, ProtoOATrendbarPeriod period)
        {
            var requestMessage = new ProtoOAUnsubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = accountId,
                Period = period,
                SymbolId = symbolId,
                PayloadType = ProtoOAPayloadType.ProtoOaUnsubscribeLiveTrendbarReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAUnsubscribeLiveTrendbarRes res = (ProtoOAUnsubscribeLiveTrendbarRes)message;
            return res;
        }


        /// <summary>
        /// Request for subscribing on spot events of the specified symbol.
        /// TODO:EventHandler
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<ProtoOASubscribeDepthQuotesRes> SubscribeToDepths(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new ProtoOASubscribeDepthQuotesReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaSubscribeDepthQuotesReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOASubscribeDepthQuotesRes res = (ProtoOASubscribeDepthQuotesRes)message;
            return res;
        }
        /// <summary>
        /// Request for unsubscribing from the depth of market of the specified symbol.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="symbolIds"></param>
        /// <returns></returns>
        public async Task<ProtoOAUnsubscribeDepthQuotesRes> UnsubscribeFromDepths(long accountId, IEnumerable<long> symbolIds)
        {
            var requestMessage = new ProtoOAUnsubscribeDepthQuotesReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaUnsubscribeDepthQuotesReq
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAUnsubscribeDepthQuotesRes res = (ProtoOAUnsubscribeDepthQuotesRes)message;
            return res;
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
        public async Task<ProtoOATrendbar[]> GetTrendbars(long accountId, long symbolId, ProtoOATrendbarPeriod period, DateTimeOffset from = default, DateTimeOffset to = default, uint count = default)
        {
            var periodMaximum = period.GetMaximumTime();
            if(from == default & to == default)
            {
                to = DateTimeOffset.UtcNow.AddSeconds(1);
            }
            if (to == default) to = from.Add(periodMaximum);
            if (from == default) from = to.Add(-periodMaximum);
            if (to - from > periodMaximum | to < from) throw new ArgumentOutOfRangeException(nameof(to), "The time range is not valid");
            var requestMessage = new ProtoOAGetTrendbarsReq
            {
                FromTimestamp = from.ToUnixTimeMilliseconds(),
                ToTimestamp = to.ToUnixTimeMilliseconds(),
                CtidTraderAccountId = accountId,
                Period = period,
                SymbolId = symbolId,
                PayloadType = ProtoOAPayloadType.ProtoOaGetTrendbarsReq
            };
            if(count != default) requestMessage.Count = count;
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAGetTrendbarsRes res = (ProtoOAGetTrendbarsRes)message;
            ProtoOATrendbar[] trendBars = res.Trendbar.ToArray();
            return trendBars;
        }
        /// <summary>
        /// Request for the list of assets available for a trader's account.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ProtoOAAsset[]> GetAssets(long accountId)
        {
            var requestMessage = new ProtoOAAssetListReq
            {
                CtidTraderAccountId = accountId,
                PayloadType = ProtoOAPayloadType.ProtoOaAssetListReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAAssetListRes res = (ProtoOAAssetListRes)message;
            ProtoOAAsset[] assets = res.Asset.ToArray();
            return assets;
        }
        /// <summary>
        /// Request for getting the proxy version.
        /// Can be used to check the current version of the Open API scheme.
        /// </summary>
        /// <returns></returns>
        public async Task<ProtoOAVersionRes> GetVersion()
        {
            var requestMessage = new ProtoOAVersionReq()
            {
                PayloadType = ProtoOAPayloadType.ProtoOaVersionReq
            };
            IOAMessage message = await SendMessageWaitResponse(requestMessage);
            ProtoOAVersionRes res = (ProtoOAVersionRes)message;
            return res;
        }
    }
}
