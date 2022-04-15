using Google.Protobuf;
using OpenAPI.Net;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Trading.UI.Sample.Enums;
using Trading.UI.Sample.Models;
using auth = OpenAPI.Net.Auth;

namespace Trading.UI.Sample.Services
{
    public interface IApiService : IDisposable
    {
        bool IsConnected { get; }

        IObservable<IMessage> LiveObservable { get; }

        IObservable<IMessage> DemoObservable { get; }

        Task Connect();

        Task AuthorizeApp(auth.App app);

        Task<ProtoOAAccountAuthRes> AuthorizeAccount(long accountId, bool isLive, string accessToken);

        Task<ProtoOALightSymbol[]> GetLightSymbols(long accountId, bool isLive);

        Task<ProtoOASymbol[]> GetSymbols(long accountId, bool isLive, long[] symbolIds);

        Task<SymbolModel[]> GetSymbolModels(long accountId, bool isLive, ProtoOALightSymbol[] lightSymbols, ProtoOAAsset[] assets);

        Task<ProtoOALightSymbol[]> GetConversionSymbols(long accountId, bool isLive, long baseAssetId, long quoteAssetId);

        Task<ProtoOACtidTraderAccount[]> GetAccountsList(string accessToken);

        void CreateNewOrder(OrderModel marketOrder, long accountId, bool isLive);

        void ClosePosition(long positionId, long volume, long accountId, bool isLive);

        Task<ProtoOAReconcileRes> GetAccountOrders(long accountId, bool isLive);

        void ModifyPosition(MarketOrderModel oldOrder, MarketOrderModel newOrder, long accountId, bool isLive);

        void CancelOrder(long orderId, long accountId, bool isLive);

        void ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long accountId, bool isLive);

        Task<ProtoOATrader> GetTrader(long accountId, bool isLive);

        Task<HistoricalTradeModel[]> GetHistoricalTrades(long accountId, bool isLive, DateTimeOffset from, DateTimeOffset to);

        Task<TransactionModel[]> GetTransactions(long accountId, bool isLive, DateTimeOffset from, DateTimeOffset to);

        Task<ProtoOASubscribeSpotsRes> SubscribeToSpots(long accountId, bool isLive, params long[] symbolIds);

        Task<ProtoOAUnsubscribeSpotsRes> UnsubscribeFromSpots(long accountId, bool isLive, params long[] symbolIds);

        Task<ProtoOAAsset[]> GetAssets(long accountId, bool isLive);

        Task<ProtoOATrendbar[]> GetTrendbars(long accountId, bool isLive, DateTimeOffset from, DateTimeOffset to, ProtoOATrendbarPeriod period, long symbolId);

        Task<ProtoOASubscribeLiveTrendbarRes> SubscribeToLiveTrendbar(long accountId, bool isLive, long symbolId, ProtoOATrendbarPeriod period);

        Task<ProtoOAUnsubscribeLiveTrendbarRes> UnsubscribeFromLiveTrendbar(long accountId, bool isLive, long symbolId, ProtoOATrendbarPeriod period);
    }

    public sealed class ApiService : IApiService
    {
        private readonly Func<OpenClient> _liveClientFactory;
        private readonly Func<OpenClient> _demoClientFactory;
        private readonly ConcurrentQueue<MessageQueueItem> _messagesQueue = new();
        private readonly System.Timers.Timer _sendMessageTimer;

        private OpenClient _liveClient;
        private OpenClient _demoClient;

        public ApiService(Func<OpenClient> liveClientFactory, Func<OpenClient> demoClientFactory, int maxMessagePerSecond = 45)
        {
            _liveClientFactory = liveClientFactory ?? throw new ArgumentNullException(nameof(liveClientFactory));
            _demoClientFactory = demoClientFactory ?? throw new ArgumentNullException(nameof(demoClientFactory));

            _sendMessageTimer = new(1000.0 / maxMessagePerSecond);

            _sendMessageTimer.Elapsed += SendMessageTimerElapsed;
            _sendMessageTimer.AutoReset = false;
        }

        public bool IsConnected { get; private set; }

        public IObservable<IMessage> LiveObservable => _liveClient;

        public IObservable<IMessage> DemoObservable => _demoClient;

        public async Task Connect()
        {
            OpenClient liveClient = null;
            OpenClient demoClient = null;

            try
            {
                liveClient = _liveClientFactory();

                await liveClient.Connect();

                demoClient = _demoClientFactory();

                await demoClient.Connect();
            }
            catch
            {
                if (liveClient is not null) liveClient.Dispose();
                if (demoClient is not null) demoClient.Dispose();

                throw;
            }

            _liveClient = liveClient;
            _demoClient = demoClient;

            IsConnected = true;
        }

        public async Task AuthorizeApp(auth.App app)
        {
            VerifyConnection();

            var authRequest = new ProtoOAApplicationAuthReq
            {
                ClientId = app.ClientId,
                ClientSecret = app.Secret,
            };

            bool isLiveClientAppAuthorized = false;
            bool isDemoClientAppAuthorized = false;

            using var liveDisposable = _liveClient.OfType<ProtoOAApplicationAuthRes>()
                .Subscribe(response => isLiveClientAppAuthorized = true);
            using var demoDisposable = _demoClient.OfType<ProtoOAApplicationAuthRes>()
                .Subscribe(response => isDemoClientAppAuthorized = true);

            await _liveClient.SendMessage(authRequest, ProtoOAPayloadType.ProtoOaApplicationAuthReq);
            await _demoClient.SendMessage(authRequest, ProtoOAPayloadType.ProtoOaApplicationAuthReq);

            var waitStartTime = DateTime.Now;

            while (!isLiveClientAppAuthorized && !isDemoClientAppAuthorized && DateTime.Now - waitStartTime < TimeSpan.FromSeconds(5))
            {
                await Task.Delay(1000);
            }

            if (!isLiveClientAppAuthorized || !isDemoClientAppAuthorized) throw new TimeoutException("The API didn't responded");

            _sendMessageTimer.Start();
        }

        public Task<ProtoOAAccountAuthRes> AuthorizeAccount(long accountId, bool isLive, string accessToken)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOAAccountAuthRes>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOAAccountAuthRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = accountId,
                AccessToken = accessToken,
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaAccountAuthReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOALightSymbol[]> GetLightSymbols(long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOALightSymbol[]>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOASymbolsListRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.Symbol.Where(iSymbol => iSymbol.Enabled).ToArray());

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOASymbolsListReq
            {
                CtidTraderAccountId = accountId,
                IncludeArchivedSymbols = false
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaSymbolsListReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOASymbol[]> GetSymbols(long accountId, bool isLive, long[] symbolIds)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOASymbol[]>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOASymbolByIdRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.Symbol.Where(iSymbol => iSymbol.TradingMode == ProtoOATradingMode.Enabled).ToArray());

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOASymbolByIdReq
            {
                CtidTraderAccountId = accountId,
            };

            requestMessage.SymbolId.AddRange(symbolIds);

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaSymbolByIdReq, client);

            return taskCompletionSource.Task;
        }

        public async Task<SymbolModel[]> GetSymbolModels(long accountId, bool isLive, ProtoOALightSymbol[] lightSymbols, ProtoOAAsset[] assets)
        {
            var symbolIds = lightSymbols.Select(iSymbol => iSymbol.SymbolId).ToArray();

            var symbols = await GetSymbols(accountId, isLive, symbolIds);

            return lightSymbols.Where(lightSymbol => symbols.Any(symbol => lightSymbol.SymbolId == symbol.SymbolId)).Select(lightSymbol => new SymbolModel
            {
                LightSymbol = lightSymbol,
                Data = symbols.First(symbol => symbol.SymbolId == lightSymbol.SymbolId),
                BaseAsset = assets.First(iAsset => iAsset.AssetId == lightSymbol.BaseAssetId),
                QuoteAsset = assets.First(iAsset => iAsset.AssetId == lightSymbol.QuoteAssetId)
            }).ToArray();
        }

        public Task<ProtoOALightSymbol[]> GetConversionSymbols(long accountId, bool isLive, long baseAssetId, long quoteAssetId)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOALightSymbol[]>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOASymbolsForConversionRes>().Where(response => response.CtidTraderAccountId == accountId)
                .Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.Symbol.ToArray());

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOASymbolsForConversionReq
            {
                CtidTraderAccountId = accountId,
                FirstAssetId = baseAssetId,
                LastAssetId = quoteAssetId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaSymbolsForConversionReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOACtidTraderAccount[]> GetAccountsList(string accessToken)
        {
            var taskCompletionSource = new TaskCompletionSource<ProtoOACtidTraderAccount[]>();

            IDisposable disposable = null;

            disposable = _liveClient.OfType<ProtoOAGetAccountListByAccessTokenRes>().Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.CtidTraderAccount.ToArray());

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = accessToken
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq, _liveClient);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOAReconcileRes> GetAccountOrders(long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOAReconcileRes>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOAReconcileRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAReconcileReq
            {
                CtidTraderAccountId = accountId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaReconcileReq, client);

            return taskCompletionSource.Task;
        }

        public void CreateNewOrder(OrderModel orderModel, long accountId, bool isLive)
        {
            VerifyConnection();

            var newOrderReq = new ProtoOANewOrderReq
            {
                CtidTraderAccountId = accountId,
                SymbolId = orderModel.Symbol.Id,
                Volume = orderModel.Volume,
                TradeSide = orderModel.TradeSide,
            };

            if (orderModel is MarketOrderModel marketOrder)
            {
                if (marketOrder.IsMarketRange)
                {
                    newOrderReq.OrderType = ProtoOAOrderType.MarketRange;

                    newOrderReq.BaseSlippagePrice = marketOrder.Symbol.Bid;

                    var slippageinPoint = orderModel.Symbol.Data.GetPointsFromPips(marketOrder.MarketRangeInPips);

                    if (slippageinPoint < int.MaxValue) newOrderReq.SlippageInPoints = (int)slippageinPoint;
                }
                else
                {
                    newOrderReq.OrderType = ProtoOAOrderType.Market;
                }

                if (marketOrder.Id != default)
                {
                    newOrderReq.PositionId = marketOrder.Id;
                }
            }
            else if (orderModel is PendingOrderModel pendingOrder)
            {
                newOrderReq.OrderType = pendingOrder.ProtoType;

                if (newOrderReq.OrderType == ProtoOAOrderType.Limit)
                {
                    newOrderReq.LimitPrice = pendingOrder.Price;
                }
                else
                {
                    newOrderReq.StopPrice = pendingOrder.Price;

                    if (newOrderReq.OrderType == ProtoOAOrderType.StopLimit)
                    {
                        var slippageinPoint = pendingOrder.Symbol.Data.GetPointsFromPips(pendingOrder.LimitRangeInPips);

                        if (slippageinPoint < int.MaxValue) newOrderReq.SlippageInPoints = (int)slippageinPoint;
                    }
                }

                if (pendingOrder.IsExpiryEnabled)
                {
                    newOrderReq.ExpirationTimestamp = new DateTimeOffset(pendingOrder.ExpiryTime).ToUnixTimeMilliseconds();
                }
            }

            if (orderModel.IsStopLossEnabled)
            {
                newOrderReq.TrailingStopLoss = orderModel.IsTrailingStopLossEnabled;

                newOrderReq.RelativeStopLoss = orderModel.RelativeStopLoss;
            }

            if (orderModel.IsTakeProfitEnabled)
            {
                newOrderReq.RelativeTakeProfit = orderModel.RelativeTakeProfit;
            }

            if (string.IsNullOrWhiteSpace(orderModel.Label) is not true)
            {
                newOrderReq.Label = orderModel.Label;
            }

            if (string.IsNullOrWhiteSpace(orderModel.Comment) is not true)
            {
                newOrderReq.Comment = orderModel.Comment;
            }

            var client = GetClient(isLive);

            EnqueueMessage(newOrderReq, ProtoOAPayloadType.ProtoOaNewOrderReq, client);
        }

        public void ClosePosition(long positionId, long volume, long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var requestMessage = new ProtoOAClosePositionReq
            {
                CtidTraderAccountId = accountId,
                PositionId = positionId,
                Volume = volume
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaClosePositionReq, client);
        }

        public void CancelOrder(long orderId, long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var requestMessage = new ProtoOACancelOrderReq
            {
                CtidTraderAccountId = accountId,
                OrderId = orderId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaCancelOrderReq, client);
        }

        public void ModifyPosition(MarketOrderModel oldOrder, MarketOrderModel newOrder, long accountId, bool isLive)
        {
            VerifyConnection();

            if (oldOrder.TradeData.TradeSide != newOrder.TradeSide)
            {
                ClosePosition(oldOrder.Id, oldOrder.Volume, accountId, isLive);

                newOrder.Id = default;

                CreateNewOrder(newOrder, accountId, isLive);
            }
            else
            {
                if (newOrder.Volume > oldOrder.Volume)
                {
                    newOrder.Volume -= oldOrder.Volume;

                    CreateNewOrder(newOrder, accountId, isLive);
                }
                else
                {
                    if (newOrder.StopLossInPips != oldOrder.StopLossInPips || newOrder.IsStopLossEnabled != oldOrder.IsStopLossEnabled || newOrder.TakeProfitInPips != oldOrder.TakeProfitInPips || newOrder.IsTakeProfitEnabled != oldOrder.IsTakeProfitEnabled
                        || newOrder.IsTrailingStopLossEnabled != oldOrder.IsTrailingStopLossEnabled)
                    {
                        var amendPositionRequestMessage = new ProtoOAAmendPositionSLTPReq
                        {
                            PositionId = oldOrder.Id,
                            CtidTraderAccountId = accountId,
                            GuaranteedStopLoss = oldOrder.GuaranteedStopLoss,
                        };

                        if (newOrder.IsStopLossEnabled)
                        {
                            if (newOrder.StopLossInPips != default && newOrder.StopLossInPrice == default)
                            {
                                newOrder.StopLossInPrice = newOrder.TradeSide == ProtoOATradeSide.Sell
                                    ? newOrder.Symbol.Data.AddPipsToPrice(newOrder.Price, newOrder.StopLossInPips)
                                    : newOrder.Symbol.Data.SubtractPipsFromPrice(newOrder.Price, newOrder.StopLossInPips);
                            }

                            amendPositionRequestMessage.StopLoss = newOrder.StopLossInPrice;
                            amendPositionRequestMessage.StopLossTriggerMethod = oldOrder.StopTriggerMethod;
                            amendPositionRequestMessage.TrailingStopLoss = newOrder.IsTrailingStopLossEnabled;
                        }

                        if (newOrder.IsTakeProfitEnabled)
                        {
                            if (newOrder.TakeProfitInPips != default && newOrder.TakeProfitInPrice == default)
                            {
                                newOrder.TakeProfitInPrice = newOrder.TradeSide == ProtoOATradeSide.Sell
                                    ? newOrder.Symbol.Data.SubtractPipsFromPrice(newOrder.Price, newOrder.TakeProfitInPips)
                                    : newOrder.Symbol.Data.AddPipsToPrice(newOrder.Price, newOrder.TakeProfitInPips);
                            }

                            amendPositionRequestMessage.TakeProfit = newOrder.TakeProfitInPrice;
                        }

                        var client = GetClient(isLive);

                        EnqueueMessage(amendPositionRequestMessage, ProtoOAPayloadType.ProtoOaAmendPositionSltpReq, client);
                    }

                    if (newOrder.Volume < oldOrder.Volume)
                    {
                        ClosePosition(oldOrder.Id, oldOrder.Volume - newOrder.Volume, accountId, isLive);
                    }
                }
            }
        }

        public Task<ProtoOATrader> GetTrader(long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOATrader>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOATraderRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.Trader);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOATraderReq
            {
                CtidTraderAccountId = accountId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaTraderReq, client);

            return taskCompletionSource.Task;
        }

        public void ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long accountId, bool isLive)
        {
            VerifyConnection();

            var requestMessage = new ProtoOAAmendOrderReq
            {
                OrderId = oldOrder.Id,
                CtidTraderAccountId = accountId,
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
                requestMessage.ExpirationTimestamp = new DateTimeOffset(newOrder.ExpiryTime).ToUnixTimeMilliseconds();
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

            var client = GetClient(isLive);

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaAmendOrderReq, client);
        }

        public Task<HistoricalTradeModel[]> GetHistoricalTrades(long accountId, bool isLive, DateTimeOffset from, DateTimeOffset to)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            List<HistoricalTradeModel> result = new();

            var taskCompletionSource = new TaskCompletionSource<HistoricalTradeModel[]>();

            var requestsNumber = 0;
            var responsesNumber = 0;

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOADealListRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                var trades = response.Deal.Select(deal =>
                {
                    var trade = new HistoricalTradeModel
                    {
                        Id = deal.DealId,
                        SymbolId = deal.SymbolId,
                        OrderId = deal.OrderId,
                        PositionId = deal.PositionId,
                        Volume = deal.Volume,
                        FilledVolume = deal.FilledVolume,
                        TradeSide = deal.TradeSide,
                        Status = deal.DealStatus,
                        Commission = deal.Commission,
                        ExecutionPrice = deal.ExecutionPrice,
                        MarginRate = deal.MarginRate,
                        BaseToUsdConversionRate = deal.BaseToUsdConversionRate,
                        CreationTime = DateTimeOffset.FromUnixTimeMilliseconds(deal.CreateTimestamp),
                        ExecutionTime = DateTimeOffset.FromUnixTimeMilliseconds(deal.ExecutionTimestamp),
                        LastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(deal.UtcLastUpdateTimestamp)
                    };

                    if (deal.ClosePositionDetail != null)
                    {
                        trade.IsClosing = true;
                        trade.ClosedVolume = deal.ClosePositionDetail.ClosedVolume;
                        trade.GrossProfit = deal.ClosePositionDetail.GrossProfit;
                        trade.Swap = deal.ClosePositionDetail.Swap;
                        trade.ClosedBalance = deal.ClosePositionDetail.Balance;
                        trade.QuoteToDepositConversionRate = deal.ClosePositionDetail.QuoteToDepositConversionRate;
                    }
                    else
                    {
                        trade.IsClosing = false;
                    }

                    return trade;
                });

                result.AddRange(trades);

                responsesNumber++;

                if (responsesNumber == requestsNumber)
                {
                    taskCompletionSource.SetResult(result.ToArray());

                    disposable?.Dispose();
                }
            });

            var timeAmountToAdd = TimeSpan.FromMilliseconds(604800000);

            var time = from;

            do
            {
                var toTime = time.Add(timeAmountToAdd);

                var requestMessage = new ProtoOADealListReq
                {
                    FromTimestamp = time.ToUnixTimeMilliseconds(),
                    ToTimestamp = toTime.ToUnixTimeMilliseconds(),
                    CtidTraderAccountId = accountId
                };

                EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaDealListReq, client);

                time = toTime;

                requestsNumber++;
            } while (time < to);

            return taskCompletionSource.Task;
        }

        public Task<TransactionModel[]> GetTransactions(long accountId, bool isLive, DateTimeOffset from, DateTimeOffset to)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            List<TransactionModel> result = new();

            var taskCompletionSource = new TaskCompletionSource<TransactionModel[]>();

            var requestsNumber = 0;
            var responsesNumber = 0;

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOACashFlowHistoryListRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                var transactions = response.DepositWithdraw.Select(depositWithdraw => new TransactionModel
                {
                    Id = depositWithdraw.BalanceHistoryId,
                    Type = depositWithdraw.OperationType,
                    Balance = depositWithdraw.Balance,
                    BalanceVersion = depositWithdraw.BalanceVersion,
                    Delta = depositWithdraw.Delta,
                    Equity = depositWithdraw.Equity,
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(depositWithdraw.ChangeBalanceTimestamp),
                    Note = string.IsNullOrEmpty(depositWithdraw.ExternalNote) ? string.Empty : depositWithdraw.ExternalNote
                });

                result.AddRange(transactions);

                responsesNumber++;

                if (responsesNumber == requestsNumber)
                {
                    taskCompletionSource.SetResult(result.ToArray());

                    disposable?.Dispose();
                }
            });

            var timeAmountToAdd = TimeSpan.FromMilliseconds(604800000);

            var time = from;

            do
            {
                var toTime = time.Add(timeAmountToAdd);

                var requestMessage = new ProtoOACashFlowHistoryListReq
                {
                    FromTimestamp = time.ToUnixTimeMilliseconds(),
                    ToTimestamp = toTime.ToUnixTimeMilliseconds(),
                    CtidTraderAccountId = accountId
                };

                EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaCashFlowHistoryListReq, client);

                time = toTime;

                requestsNumber++;
            } while (time < to);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOASubscribeSpotsRes> SubscribeToSpots(long accountId, bool isLive, params long[] symbolIds)
        {
            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOASubscribeSpotsRes>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOASubscribeSpotsRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOASubscribeSpotsReq
            {
                CtidTraderAccountId = accountId,
            };

            requestMessage.SymbolId.AddRange(symbolIds);

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaSubscribeSpotsReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOAUnsubscribeSpotsRes> UnsubscribeFromSpots(long accountId, bool isLive, params long[] symbolIds)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOAUnsubscribeSpotsRes>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOAUnsubscribeSpotsRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAUnsubscribeSpotsReq
            {
                CtidTraderAccountId = accountId,
            };

            requestMessage.SymbolId.AddRange(symbolIds);

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaUnsubscribeSpotsReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOASubscribeLiveTrendbarRes> SubscribeToLiveTrendbar(long accountId, bool isLive, long symbolId, ProtoOATrendbarPeriod period)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOASubscribeLiveTrendbarRes>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOASubscribeLiveTrendbarRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOASubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = accountId,
                Period = period,
                SymbolId = symbolId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaSubscribeLiveTrendbarReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOAUnsubscribeLiveTrendbarRes> UnsubscribeFromLiveTrendbar(long accountId, bool isLive, long symbolId, ProtoOATrendbarPeriod period)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOAUnsubscribeLiveTrendbarRes>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOAUnsubscribeLiveTrendbarRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response);

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAUnsubscribeLiveTrendbarReq
            {
                CtidTraderAccountId = accountId,
                Period = period,
                SymbolId = symbolId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaUnsubscribeLiveTrendbarReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOATrendbar[]> GetTrendbars(long accountId, bool isLive, DateTimeOffset from, DateTimeOffset to, ProtoOATrendbarPeriod period, long symbolId)
        {
            VerifyConnection();

            var periodMaximum = period.GetMaximumTime();

            if (from == default) from = to.Add(-periodMaximum);

            if (to - from > periodMaximum) throw new ArgumentOutOfRangeException(nameof(to), "The time range is not valid");

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOATrendbar[]>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOAGetTrendbarsRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.Trendbar.ToArray());

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAGetTrendbarsReq
            {
                FromTimestamp = from.ToUnixTimeMilliseconds(),
                ToTimestamp = to.ToUnixTimeMilliseconds(),
                CtidTraderAccountId = accountId,
                Period = period,
                SymbolId = symbolId
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaGetTrendbarsReq, client);

            return taskCompletionSource.Task;
        }

        public Task<ProtoOAAsset[]> GetAssets(long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var taskCompletionSource = new TaskCompletionSource<ProtoOAAsset[]>();

            IDisposable disposable = null;

            disposable = client.OfType<ProtoOAAssetListRes>().Where(response => response.CtidTraderAccountId == accountId).Subscribe(response =>
            {
                taskCompletionSource.SetResult(response.Asset.ToArray());

                disposable?.Dispose();
            });

            var requestMessage = new ProtoOAAssetListReq
            {
                CtidTraderAccountId = accountId,
            };

            EnqueueMessage(requestMessage, ProtoOAPayloadType.ProtoOaAssetListReq, client);

            return taskCompletionSource.Task;
        }

        public void Dispose()
        {
            _liveClient?.Dispose();
            _demoClient?.Dispose();
        }

        private OpenClient GetClient(bool isLive) => isLive ? _liveClient : _demoClient;

        private void VerifyConnection()
        {
            if (IsConnected is false) throw new InvalidOperationException("The service is not connected yet, please connect the service before using it");
        }

        private void EnqueueMessage<TMessage>(TMessage message, ProtoOAPayloadType payloadType, OpenClient client)
            where TMessage : IMessage
        {
            var messageQueueItem = new MessageQueueItem
            {
                Message = message,
                PayloadType = payloadType,
                Client = client,
                IsHistorical = payloadType is ProtoOAPayloadType.ProtoOaDealListReq or ProtoOAPayloadType.ProtoOaGetTrendbarsReq or ProtoOAPayloadType.ProtoOaGetTickdataReq or ProtoOAPayloadType.ProtoOaCashFlowHistoryListReq
            };

            _messagesQueue.Enqueue(messageQueueItem);
        }

        private async void SendMessageTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _sendMessageTimer.Stop();

            if (_messagesQueue.TryDequeue(out var messageQueueItem) == false)
            {
                _sendMessageTimer.Start();

                return;
            }

            await messageQueueItem.Client.SendMessage(messageQueueItem.Message, messageQueueItem.PayloadType);

            if (messageQueueItem.IsHistorical) await Task.Delay(250);

            _sendMessageTimer.Start();
        }

        private class MessageQueueItem
        {
            public IMessage Message { get; init; }

            public ProtoOAPayloadType PayloadType { get; init; }

            public OpenClient Client { get; init; }

            public bool IsHistorical { get; init; }
        }
    }
}