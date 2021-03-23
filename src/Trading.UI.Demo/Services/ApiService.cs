using Google.Protobuf;
using OpenAPI.Net;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trading.UI.Demo.Enums;
using Trading.UI.Demo.Models;
using auth = OpenAPI.Net.Auth;

namespace Trading.UI.Demo.Services
{
    public interface IApiService : IDisposable
    {
        bool IsConnected { get; }

        IObservable<IMessage> LiveObservable { get; }

        IObservable<IMessage> DemoObservable { get; }

        Task Connect();

        Task AuthorizeApp(auth.App app);

        Task<ProtoOAAccountAuthRes> AuthorizeAccount(ProtoOACtidTraderAccount account, string accessToken);

        Task<ProtoOALightSymbol[]> GetLightSymbols(ProtoOACtidTraderAccount account);

        Task<ProtoOASymbol[]> GetSymbols(ProtoOACtidTraderAccount account, long[] symbolIds);

        Task<SymbolModel[]> GetSymbolModels(ProtoOACtidTraderAccount account);

        Task<ProtoOACtidTraderAccount[]> GetAccountsList(string accessToken);

        Task CreateNewOrder(OrderModel marketOrder, long accountId, bool isLive);

        Task ClosePosition(long positionId, long volume, long accountId, bool isLive);

        Task<ProtoOAReconcileRes> GetAccountOrders(long accountId, bool isLive);

        Task ModifyPosition(MarketOrderModel oldOrder, MarketOrderModel newOrder, long accountId, bool isLive);

        Task CancelOrder(long orderId, long accountId, bool isLive);

        Task ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long accountId, bool isLive);
    }

    public sealed class ApiService : IApiService
    {
        private readonly Func<OpenClient> _liveClientFactory;
        private readonly Func<OpenClient> _demoClientFactory;

        private OpenClient _liveClient;
        private OpenClient _demoClient;

        public ApiService(Func<OpenClient> liveClientFactory, Func<OpenClient> demoClientFactory)
        {
            _liveClientFactory = liveClientFactory ?? throw new ArgumentNullException(nameof(liveClientFactory));
            _demoClientFactory = demoClientFactory ?? throw new ArgumentNullException(nameof(demoClientFactory));
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
        }

        public async Task<ProtoOAAccountAuthRes> AuthorizeAccount(ProtoOACtidTraderAccount account, string accessToken)
        {
            VerifyConnection();

            var accountId = (long)account.CtidTraderAccountId;

            var client = GetClient(account.IsLive);

            using var cancelationTokenSource = new CancellationTokenSource();

            ProtoOAAccountAuthRes result = null;

            using var disposable = client.OfType<ProtoOAAccountAuthRes>().Where(response => response.CtidTraderAccountId == accountId)
                .Subscribe(response =>
                {
                    result = response;

                    cancelationTokenSource.Cancel();
                });

            var message = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = (long)account.CtidTraderAccountId,
                AccessToken = accessToken,
            };

            await client.SendMessage(message, ProtoOAPayloadType.ProtoOaAccountAuthReq);

            await DelayUntilCanceled(TimeSpan.FromMinutes(1), cancelationTokenSource.Token);

            if (result is null) throw new TimeoutException("The API didn't responded");

            return result;
        }

        public async Task<ProtoOALightSymbol[]> GetLightSymbols(ProtoOACtidTraderAccount account)
        {
            VerifyConnection();

            var accountId = (long)account.CtidTraderAccountId;

            var client = GetClient(account.IsLive);

            using var cancelationTokenSource = new CancellationTokenSource();

            ProtoOALightSymbol[] result = null;

            using var disposable = client.OfType<ProtoOASymbolsListRes>().Where(response => response.CtidTraderAccountId == accountId)
                .Subscribe(response =>
            {
                result = response.Symbol.Where(iSymbol => iSymbol.Enabled).ToArray();

                cancelationTokenSource.Cancel();
            });

            var symbolsMessage = new ProtoOASymbolsListReq
            {
                CtidTraderAccountId = (long)account.CtidTraderAccountId,
                IncludeArchivedSymbols = false
            };

            await client.SendMessage(symbolsMessage, ProtoOAPayloadType.ProtoOaSymbolsListReq);

            await DelayUntilCanceled(TimeSpan.FromMinutes(1), cancelationTokenSource.Token);

            if (result is null) throw new TimeoutException("The API didn't responded");

            return result;
        }

        public async Task<ProtoOASymbol[]> GetSymbols(ProtoOACtidTraderAccount account, long[] symbolIds)
        {
            VerifyConnection();

            var accountId = (long)account.CtidTraderAccountId;

            var client = GetClient(account.IsLive);

            using var cancelationTokenSource = new CancellationTokenSource();

            ProtoOASymbol[] result = null;

            using var disposable = client.OfType<ProtoOASymbolByIdRes>().Where(response => response.CtidTraderAccountId == accountId)
                .Subscribe(response =>
            {
                result = response.Symbol.Where(iSymbol => iSymbol.TradingMode == ProtoOATradingMode.Enabled).ToArray();

                cancelationTokenSource.Cancel();
            });

            var symbolsMessage = new ProtoOASymbolByIdReq
            {
                CtidTraderAccountId = (long)account.CtidTraderAccountId,
            };

            symbolsMessage.SymbolId.AddRange(symbolIds);

            await client.SendMessage(symbolsMessage, ProtoOAPayloadType.ProtoOaSymbolByIdReq);

            await DelayUntilCanceled(TimeSpan.FromMinutes(1), cancelationTokenSource.Token);

            if (result is null) throw new TimeoutException("The API didn't responded");

            return result;
        }

        public async Task<SymbolModel[]> GetSymbolModels(ProtoOACtidTraderAccount account)
        {
            VerifyConnection();

            var lightSymbols = await GetLightSymbols(account);

            var symbolIds = lightSymbols.Select(iSymbol => iSymbol.SymbolId).ToArray();

            var symbols = await GetSymbols(account, symbolIds);

            return lightSymbols.Where(lightSymbol => symbols.Any(symbol => lightSymbol.SymbolId == symbol.SymbolId)).Select(lightSymbol => new SymbolModel
            {
                LightSymbol = lightSymbol,
                Data = symbols.First(symbol => symbol.SymbolId == lightSymbol.SymbolId)
            }).ToArray();
        }

        public async Task<ProtoOACtidTraderAccount[]> GetAccountsList(string accessToken)
        {
            var accountListRequest = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = accessToken
            };

            using var cancelationTokenSource = new CancellationTokenSource();

            ProtoOACtidTraderAccount[] result = null;

            using var disposable = _liveClient.OfType<ProtoOAGetAccountListByAccessTokenRes>()
                .Subscribe(response =>
                {
                    result = response.CtidTraderAccount.ToArray();

                    cancelationTokenSource.Cancel();
                });

            await _liveClient.SendMessage(accountListRequest, ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq);

            await DelayUntilCanceled(TimeSpan.FromMinutes(1), cancelationTokenSource.Token);

            if (result is null) throw new TimeoutException("The API didn't responded");

            return result;
        }

        public async Task CreateNewOrder(OrderModel orderModel, long accountId, bool isLive)
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
                newOrderReq.OrderType = marketOrder.IsMarketRange ? ProtoOAOrderType.MarketRange : ProtoOAOrderType.Market;

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
                        var slippageinPoint = pendingOrder.Symbol.GetPointsFromPips(pendingOrder.LimitRangeInPips);

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

            await client.SendMessage(newOrderReq, ProtoOAPayloadType.ProtoOaNewOrderReq);
        }

        public Task ClosePosition(long positionId, long volume, long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var requestMessage = new ProtoOAClosePositionReq
            {
                CtidTraderAccountId = accountId,
                PositionId = positionId,
                Volume = volume
            };

            return client.SendMessage(requestMessage, ProtoOAPayloadType.ProtoOaClosePositionReq);
        }

        public Task CancelOrder(long orderId, long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            var requestMessage = new ProtoOACancelOrderReq
            {
                CtidTraderAccountId = accountId,
                OrderId = orderId
            };

            return client.SendMessage(requestMessage, ProtoOAPayloadType.ProtoOaCancelOrderReq);
        }

        public async Task<ProtoOAReconcileRes> GetAccountOrders(long accountId, bool isLive)
        {
            VerifyConnection();

            var client = GetClient(isLive);

            using var cancelationTokenSource = new CancellationTokenSource();

            ProtoOAReconcileRes result = null;

            using var disposable = client.OfType<ProtoOAReconcileRes>().Where(response => response.CtidTraderAccountId == accountId)
                .Subscribe(response =>
            {
                result = response;

                cancelationTokenSource.Cancel();
            });

            var requestMessage = new ProtoOAReconcileReq
            {
                CtidTraderAccountId = accountId
            };

            await client.SendMessage(requestMessage, ProtoOAPayloadType.ProtoOaReconcileReq);

            await DelayUntilCanceled(TimeSpan.FromMinutes(1), cancelationTokenSource.Token);

            if (result is null) throw new TimeoutException("The API didn't responded");

            return result;
        }

        public async Task ModifyPosition(MarketOrderModel oldOrder, MarketOrderModel newOrder, long accountId, bool isLive)
        {
            VerifyConnection();

            if (oldOrder.TradeData.TradeSide != newOrder.TradeSide)
            {
                await ClosePosition(oldOrder.Id, oldOrder.Volume, accountId, isLive);

                newOrder.Id = default;

                await CreateNewOrder(newOrder, accountId, isLive);
            }
            else
            {
                if (newOrder.Volume > oldOrder.Volume)
                {
                    newOrder.Volume = newOrder.Volume - oldOrder.Volume;

                    await CreateNewOrder(newOrder, accountId, isLive);
                }
                else
                {
                    if (newOrder.StopLossInPips != oldOrder.StopLossInPips || newOrder.TakeProfitInPips != oldOrder.TakeProfitInPips)
                    {
                        var amendPositionRequestMessage = new ProtoOAAmendPositionSLTPReq
                        {
                            PositionId = oldOrder.Id,
                            CtidTraderAccountId = accountId,
                            GuaranteedStopLoss = oldOrder.GuaranteedStopLoss,
                        };

                        if (newOrder.IsStopLossEnabled)
                        {
                            amendPositionRequestMessage.StopLoss = newOrder.StopLossInPrice;
                            amendPositionRequestMessage.StopLossTriggerMethod = oldOrder.StopTriggerMethod;
                            amendPositionRequestMessage.TrailingStopLoss = newOrder.IsTrailingStopLossEnabled;
                        }

                        if (newOrder.IsTakeProfitEnabled)
                        {
                            amendPositionRequestMessage.TakeProfit = newOrder.TakeProfitInPrice;
                        }

                        var client = GetClient(isLive);

                        await client.SendMessage(amendPositionRequestMessage, ProtoOAPayloadType.ProtoOaAmendPositionSltpReq);
                    }

                    if (newOrder.Volume < oldOrder.Volume)
                    {
                        await ClosePosition(oldOrder.Id, oldOrder.Volume - newOrder.Volume, accountId, isLive);
                    }
                }
            }
        }

        private OpenClient GetClient(bool isLive) => isLive ? _liveClient : _demoClient;

        public void Dispose()
        {
            _liveClient?.Dispose();
            _demoClient?.Dispose();
        }

        private void VerifyConnection()
        {
            if (IsConnected is not true) throw new InvalidOperationException("The API service is not connected yet, please connect the service before calling this method");
        }

        private async Task DelayUntilCanceled(TimeSpan timeSpan, CancellationToken token)
        {
            try
            {
                await Task.Delay(timeSpan, token);
            }
            catch (TaskCanceledException)
            {
            }
        }

        public async Task ModifyOrder(PendingOrderModel oldOrder, PendingOrderModel newOrder, long accountId, bool isLive)
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
                requestMessage.StopLoss = newOrder.StopLossInPrice;
                requestMessage.TrailingStopLoss = newOrder.IsTrailingStopLossEnabled;
            }

            if (newOrder.IsTakeProfitEnabled)
            {
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
                    var slippageinPoint = newOrder.Symbol.GetPointsFromPips(newOrder.LimitRangeInPips);

                    if (slippageinPoint < int.MaxValue) requestMessage.SlippageInPoints = (int)slippageinPoint;
                }
            }

            var client = GetClient(isLive);

            await client.SendMessage(requestMessage, ProtoOAPayloadType.ProtoOaAmendOrderReq);
        }
    }
}