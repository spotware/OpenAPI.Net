using Google.Protobuf;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Exceptions;
using OpenAPI.Net.Helpers;
using OpenAPI.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public sealed partial class OpenClient : IDisposable, IObservable<IMessage>
    {
        private WebSocket _client;
        private readonly Func<Uri, CancellationToken, Task<WebSocket>> _connectionFactory;
        public void testNetwork()
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus == OperationalStatus.Up && iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    Console.WriteLine("Interface: {0}\t{1}\t{2}", iface.Name, iface.NetworkInterfaceType, iface.OperationalStatus);
                    foreach (var ua in iface.GetIPProperties().UnicastAddresses)
                    {
                        Console.WriteLine("Address: " + ua.Address);
                        try
                        {
                            using (var client = new TcpClient(new IPEndPoint(ua.Address, 0)))
                            {
                                client.Connect("ns1.vianett.no", 80);
                                var buf = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nConnection: close\r\nHost: ns1.vianett.no\r\n\r\n");
                                client.GetStream().Write(buf, 0, buf.Length);
                                var sr = new StreamReader(client.GetStream());
                                var all = sr.ReadToEnd();
                                var match = Regex.Match(all, "(?mi)^X-YourIP: (?'a'.+)$");
                                Console.WriteLine("Your address is " + (match.Success ? match.Groups["a"].Value : all));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Default Await Response Timeout in milliseconds, after that time
        /// null message is returned from await Task<IMessage> methods.
        /// </summary>
        public static uint DefaultAwaitResponseTimeout = 60000;
        /// <summary>
        /// lastTimeStamp is expressed in 1/100th of seconds
        /// </summary>
        private static ulong lastTimeStamp = (ulong)(DateTime.UtcNow.Ticks / 100000);
        public static ulong NewMessageUniqueID
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

        /// <summary>
        /// Request for a list of symbols available for a trading account.
        /// Symbol entries are returned with the limited set of fields.
        /// </summary>
        /// <param name="cTraderAccountId"></param>
        /// <param name="includeArchivedSymbols"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<ProtoOASymbolsListRes> GetSymbolList(long cTraderAccountId, bool includeArchivedSymbols = false, int timeout = 0)
        {
            var request = new ProtoOASymbolsListReq
            {
                CtidTraderAccountId = cTraderAccountId,
                IncludeArchivedSymbols = includeArchivedSymbols
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOASymbolsListRes res = (ProtoOASymbolsListRes)message;
            return res;
        }

        private async Task<IMessage> SendMessageWaitResponse(IMessage message, int timeout = 0)
        {
            if (timeout <= 0) timeout = (int)DefaultAwaitResponseTimeout;
            ulong id = NewMessageUniqueID;
            ResultPointer rp = new ResultPointer();
            _ = resultPointers.TryAdd(id, rp);
            string clientMsgId = id.ToString();
            Console.WriteLine($"SendMessageWaitResponse:{clientMsgId}");
            await SendMessage(message, clientMsgId);
            if(!rp.WaitHandle.WaitOne(timeout))
            {
                // timeout occured - Remove current ResultPointer
                resultPointers.TryRemove(id, out ResultPointer rp2);
            }
            IMessage resultMessage = rp.Message;
            rp.Dispose();
            switch(resultMessage)
            {
                case ProtoErrorRes protoErrorRes:
                    throw new ProtoErrorResException(protoErrorRes);
                case ProtoOAErrorRes protoOAErrorRes:
                    throw new ProtoOAErrorResException(protoOAErrorRes);
                case ProtoOAOrderErrorEvent protoOAOrderErrorEvent:
                    throw new ProtoOAOrderErrorEventException(protoOAOrderErrorEvent);
            }
            return resultMessage;
        }
        private void MessageForwardByClientMsgId(IMessage message, string ClientMsgId)
        {
            Console.WriteLine($"MessageForwardByClientMsgId:{ClientMsgId}");
            Console.WriteLine(message.GetType().Name);
            if (ulong.TryParse(ClientMsgId, out ulong id))
            {
                if (resultPointers.TryRemove(id, out ResultPointer rp))
                {
                    rp.Message = message;
                    rp.WaitHandle.Set();
                }
                //else
                //{
                //    //Some requests have 2 responses, only 1-st  is handled
                //    // second goes here
                //}
            }
        }
        public async Task<Token> RefreshToken(Token token, int timeout = 0)
        {
            var request = new ProtoOARefreshTokenReq()
            {
                RefreshToken = token.RefreshToken
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOARefreshTokenRes res = (ProtoOARefreshTokenRes)message;
            token.AccessToken = res.AccessToken;
            return token;
        }

        /// <summary>
        /// Request for getting Trader's current open positions and pending orders data.
        /// </summary>
        /// <param name="cTraderAccountId"></param>
        public async Task<ProtoOAReconcileRes> GetOpenPositionsOrders(long cTraderAccountId, int timeout = 0)
        {
            var request = new ProtoOAReconcileReq
            {
                CtidTraderAccountId = cTraderAccountId,
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAReconcileRes res = (ProtoOAReconcileRes)message;
            return res;
        }
        /// <summary>
        /// Request for getting the list of granted trader's account for the access token.
        /// </summary>
        /// <param name="token"></param>
        public async Task<ProtoOACtidTraderAccount[]> GetAccountList(Token token, int timeout = 0)
        {
            Console.WriteLine("Sending ProtoOAGetAccountListByAccessTokenReq...");

            var request = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = token.AccessToken,
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAGetAccountListByAccessTokenRes res = (ProtoOAGetAccountListByAccessTokenRes)message;
            return res.CtidTraderAccount.ToArray();
        }
        /// <summary>
        /// Request for the authorizing Application.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="secret"></param>
        public async Task<ProtoOAApplicationAuthRes> ApplicationAuthorize(string clientId, string secret, int timeout = 0)
        {
            Console.WriteLine("Sending ProtoOAApplicationAuthReq...");

            var request = new ProtoOAApplicationAuthReq
            {
                ClientId = clientId,
                ClientSecret = secret
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAApplicationAuthRes res = (ProtoOAApplicationAuthRes)message;
            return res;
        }
        /// <summary>
        /// Request for the authorizing trading account session.
        /// Requires established authorized connection with the client application
        ///  using ProtoOAApplicationAuthReq.
        /// </summary>
        /// <param name="cTraderAccountId"></param>
        /// <param name="token"></param>
        public async Task<ProtoOAAccountAuthRes> AccountAuthorize(long cTraderAccountId, Token token, int timeout = 0)
        {
            Console.WriteLine("Sending ProtoOAAccountAuthReq...");

            var request = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = cTraderAccountId,
                AccessToken = token.AccessToken
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAAccountAuthRes res = (ProtoOAAccountAuthRes)message;
            return res;
        }
        public async Task<ProtoOAAccountLogoutRes> AccountLogout(long cTraderAccountId, int timeout = 0)
        {
            var request = new ProtoOAAccountLogoutReq
            {
                CtidTraderAccountId = cTraderAccountId,
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAAccountLogoutRes res = (ProtoOAAccountLogoutRes)message;
            return res;
        }

        /// <summary>
        /// Request for getting historical tick data for the symbol. Maximum is 1 week.
        /// </summary>
        /// <param name="cTraderAccountId">Unique identifier of the trader's account. Used to match responses to trader's accounts.</param>
        /// <param name="SymbolId">Unique identifier of the Symbol in cTrader platform.</param>
        /// <param name="FromDateTime">The exact UTC time of starting the search</param>
        /// <param name="ToDateTime">The exact UTC time of finishing the search</param>
        /// <param name="Type">Bid or Ask</param>
        public async Task<ProtoOAGetTickDataRes> GetTickData(long cTraderAccountId, long SymbolId,
            DateTimeOffset FromDateTime, DateTimeOffset ToDateTime, ProtoOAQuoteType Type, int timeout = 0)
        {
            Console.WriteLine("Sending ProtoOAGetTickDataReq...");
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var request = new ProtoOAGetTickDataReq
            {
                CtidTraderAccountId = cTraderAccountId,
                SymbolId = SymbolId,
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                Type = Type
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAGetTickDataRes res = (ProtoOAGetTickDataRes)message;
            return res;
        }
        /// <summary>
        /// Request for getting a full symbol entity.
        /// </summary>
        /// <param name="cTraderAccountId"></param>
        /// <param name="SymbolIds"></param>
        public async Task<ProtoOASymbolByIdRes> GetSymbolsFull(long cTraderAccountId,
            IEnumerable<long> SymbolIds, int timeout = 0)
        {
            var request = new ProtoOASymbolByIdReq
            {
                CtidTraderAccountId = cTraderAccountId,
            };
            request.SymbolId.AddRange(SymbolIds);

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOASymbolByIdRes res = (ProtoOASymbolByIdRes)message;
            return res;
        }

        public async Task<ProtoOACtidProfile> GetProfile(Token token, int timeout = 0)
        {
            var request = new ProtoOAGetCtidProfileByTokenReq
            {
                AccessToken = token.AccessToken
            };

            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOAGetCtidProfileByTokenRes res = (ProtoOAGetCtidProfileByTokenRes)message;
            return res.Profile;
        }

        public async Task<ProtoOALightSymbol[]> GetSymbolsForConversion(long cTraderAccountId, long baseAssetId, long quoteAssetId, int timeout = 0)
        {
            var request = new ProtoOASymbolsForConversionReq
            {
                CtidTraderAccountId = cTraderAccountId,
                FirstAssetId = baseAssetId,
                LastAssetId = quoteAssetId
            };
            IMessage message = await SendMessageWaitResponse(request, timeout);
            ProtoOASymbolsForConversionRes res = (ProtoOASymbolsForConversionRes)message;
            return res.Symbol.ToArray();
        }

        public async Task<ProtoOAExecutionEvent> CreateNewOrder(OrderModel orderModel, long cTraderAccountId, int timeout = 0)
        {
            ulong id = NewMessageUniqueID;
            string clientMsgId = id.ToString();
            var requestMessage = new ProtoOANewOrderReq
            {
                CtidTraderAccountId = cTraderAccountId,
                SymbolId = orderModel.Symbol.Id,
                Volume = orderModel.Volume,
                TradeSide = orderModel.TradeSide,
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
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }

        public async Task<ProtoOAExecutionEvent> ClosePosition(long positionId, long volume, long cTraderAccountId, int timeout = 0)
        {
            var requestMessage = new ProtoOAClosePositionReq
            {
                CtidTraderAccountId = cTraderAccountId,
                PositionId = positionId,
                Volume = volume
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        public async Task<ProtoOAExecutionEvent> CancelOrder(long orderId, long cTraderAccountId, int timeout = 0)
        {
            var requestMessage = new ProtoOACancelOrderReq
            {
                CtidTraderAccountId = cTraderAccountId,
                OrderId = orderId
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        //public void ModifyPosition(MarketOrderModel oldOrder, MarketOrderModel newOrder, long cTraderAccountId, int timeout = 0)
        //{
        //    if (oldOrder.TradeData.TradeSide != newOrder.TradeSide)
        //    {
        //        ClosePosition(oldOrder.Id, oldOrder.Volume, cTraderAccountId, isLive);
        //        newOrder.Id = default;
        //        CreateNewOrder(newOrder, cTraderAccountId, isLive);
        //    }
        //    else
        //    {
        //        if (newOrder.Volume > oldOrder.Volume)
        //        {
        //            newOrder.Volume -= oldOrder.Volume;
        //            CreateNewOrder(newOrder, cTraderAccountId, isLive);
        //        }
        //        else
        //        {
        //            if (newOrder.StopLossInPips != oldOrder.StopLossInPips || newOrder.IsStopLossEnabled != oldOrder.IsStopLossEnabled || newOrder.TakeProfitInPips != oldOrder.TakeProfitInPips || newOrder.IsTakeProfitEnabled != oldOrder.IsTakeProfitEnabled
        //                || newOrder.IsTrailingStopLossEnabled != oldOrder.IsTrailingStopLossEnabled)
        //            {
        //                var amendPositionRequestMessage = new ProtoOAAmendPositionSLTPReq
        //                {
        //                    PositionId = oldOrder.Id,
        //                    CtidTraderAccountId = cTraderAccountId,
        //                    GuaranteedStopLoss = oldOrder.GuaranteedStopLoss,
        //                };
        //                if (newOrder.IsStopLossEnabled)
        //                {
        //                    if (newOrder.StopLossInPips != default && newOrder.StopLossInPrice == default)
        //                    {
        //                        newOrder.StopLossInPrice = newOrder.TradeSide == ProtoOATradeSide.Sell
        //                            ? newOrder.Symbol.Data.AddPipsToPrice(newOrder.Price, newOrder.StopLossInPips)
        //                            : newOrder.Symbol.Data.SubtractPipsFromPrice(newOrder.Price, newOrder.StopLossInPips);
        //                    }
        //                    amendPositionRequestMessage.StopLoss = newOrder.StopLossInPrice;
        //                    amendPositionRequestMessage.StopLossTriggerMethod = oldOrder.StopTriggerMethod;
        //                    amendPositionRequestMessage.TrailingStopLoss = newOrder.IsTrailingStopLossEnabled;
        //                }
        //                if (newOrder.IsTakeProfitEnabled)
        //                {
        //                    if (newOrder.TakeProfitInPips != default && newOrder.TakeProfitInPrice == default)
        //                    {
        //                        newOrder.TakeProfitInPrice = newOrder.TradeSide == ProtoOATradeSide.Sell
        //                            ? newOrder.Symbol.Data.SubtractPipsFromPrice(newOrder.Price, newOrder.TakeProfitInPips)
        //                            : newOrder.Symbol.Data.AddPipsToPrice(newOrder.Price, newOrder.TakeProfitInPips);
        //                    }
        //                    amendPositionRequestMessage.TakeProfit = newOrder.TakeProfitInPrice;
        //                }
        //            }
        //            if (newOrder.Volume < oldOrder.Volume)
        //            {
        //                ClosePosition(oldOrder.Id, oldOrder.Volume - newOrder.Volume, cTraderAccountId, isLive);
        //            }
        //        }
        //    }
        //}
        public async Task<ProtoOATrader> GetTrader(long cTraderAccountId, int timeout = 0)
        {
            var requestMessage = new ProtoOATraderReq
            {
                CtidTraderAccountId = cTraderAccountId
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOATraderRes res = (ProtoOATraderRes)message;
            return res.Trader;
        }
        public async Task<ProtoOAExecutionEvent> ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long cTraderAccountId, int timeout = 0)
        {
            var requestMessage = new ProtoOAAmendOrderReq
            {
                OrderId = oldOrder.Id,
                CtidTraderAccountId = cTraderAccountId,
                Volume = newOrder.Volume,
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
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAExecutionEvent res = (ProtoOAExecutionEvent)message;
            return res;
        }
        public async Task<HistoricalTrade[]> GetHistoricalTrades(long cTraderAccountId, DateTimeOffset FromDateTime, DateTimeOffset ToDateTime, int timeout = 0)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var requestMessage = new ProtoOADealListReq
            {
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                CtidTraderAccountId = cTraderAccountId
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOADealListRes res = (ProtoOADealListRes)message;
            HistoricalTrade[] trades = res.Deal
                .Select(deal => new HistoricalTrade(deal))
                .ToArray();
            return trades;
        }
        public async Task<Transaction[]> GetTransactions(long cTraderAccountId, DateTimeOffset FromDateTime, DateTimeOffset ToDateTime, int timeout = 0)
        {
            long fromTime = FromDateTime.ToUnixTimeMilliseconds();
            long toTime = ToDateTime.ToUnixTimeMilliseconds();
            long toMax = fromTime + 604800000;
            toTime = Math.Min(toTime, toMax);
            var requestMessage = new ProtoOACashFlowHistoryListReq
            {
                FromTimestamp = fromTime,
                ToTimestamp = toTime,
                CtidTraderAccountId = cTraderAccountId
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOACashFlowHistoryListRes res = (ProtoOACashFlowHistoryListRes)message;
            Transaction[] transactions = res.DepositWithdraw
                .Select(dw => new Transaction(dw))
                .ToArray();
            return transactions;
        }
        public async Task<ProtoOASubscribeSpotsRes> SubscribeToSpots(long cTraderAccountId, IEnumerable<long> symbolIds, int timeout = 0)
        {
            var requestMessage = new ProtoOASubscribeSpotsReq
            {
                CtidTraderAccountId = cTraderAccountId,
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOASubscribeSpotsRes res = (ProtoOASubscribeSpotsRes)message;
            return res;
        }
        public async Task<ProtoOAUnsubscribeSpotsRes> UnsubscribeFromSpots(long cTraderAccountId, IEnumerable<long> symbolIds, int timeout = 0)
        {
            var requestMessage = new ProtoOAUnsubscribeSpotsReq
            {
                CtidTraderAccountId = cTraderAccountId,
            };
            requestMessage.SymbolId.AddRange(symbolIds);
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAUnsubscribeSpotsRes res = (ProtoOAUnsubscribeSpotsRes)message;
            return res;
        }
        public async Task<ProtoOASubscribeLiveTrendbarRes> SubscribeToLiveTrendbar(long cTraderAccountId, long symbolId, ProtoOATrendbarPeriod period, int timeout = 0)
        {
            var requestMessage = new ProtoOASubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = cTraderAccountId,
                Period = period,
                SymbolId = symbolId
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOASubscribeLiveTrendbarRes res = (ProtoOASubscribeLiveTrendbarRes)message;
            return res;
        }
        public async Task<ProtoOAUnsubscribeLiveTrendbarRes> UnsubscribeFromLiveTrendbar(long cTraderAccountId, long symbolId, ProtoOATrendbarPeriod period, int timeout = 0)
        {
            var requestMessage = new ProtoOAUnsubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = cTraderAccountId,
                Period = period,
                SymbolId = symbolId
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAUnsubscribeLiveTrendbarRes res = (ProtoOAUnsubscribeLiveTrendbarRes)message;
            return res;
        }
        /// <summary>
        /// Request for getting historical trend bars for the symbol.
        /// </summary>
        /// <param name="cTraderAccountId">Unique identifier of the trader's account. Used to match responses to trader's accounts.</param>
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
        public async Task<ProtoOATrendbar[]> GetTrendbars(long cTraderAccountId, long symbolId, ProtoOATrendbarPeriod period, DateTimeOffset from = default, DateTimeOffset to = default, uint count = default, int timeout = 0)
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
                CtidTraderAccountId = cTraderAccountId,
                Period = period,
                SymbolId = symbolId
            };
            if(count != default) requestMessage.Count = count;
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAGetTrendbarsRes res = (ProtoOAGetTrendbarsRes)message;
            ProtoOATrendbar[] trendBars = res.Trendbar.ToArray();
            return trendBars;
        }
        public async Task<ProtoOAAsset[]> GetAssets(long cTraderAccountId, int timeout = 0)
        {
            var requestMessage = new ProtoOAAssetListReq
            {
                CtidTraderAccountId = cTraderAccountId,
            };
            IMessage message = await SendMessageWaitResponse(requestMessage, timeout);
            ProtoOAAssetListRes res = (ProtoOAAssetListRes)message;
            ProtoOAAsset[] assets = res.Asset.ToArray();
            return assets;
        }
    }
}
