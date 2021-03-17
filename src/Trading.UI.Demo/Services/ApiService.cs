using OpenAPI.Net;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using auth = OpenAPI.Net.Auth;

namespace Trading.UI.Demo.Services
{
    public interface IApiService : IDisposable
    {
        bool IsConnected { get; }

        public IOpenClient LiveClient { get; }

        public IOpenClient DemoClient { get; }

        Task Connect();

        Task AuthorizeApp(auth.App app);
    }

    public sealed class ApiService : IApiService
    {
        private readonly Func<IOpenClient> _liveClientFactory;
        private readonly Func<IOpenClient> _demoClientFactory;

        public ApiService(Func<IOpenClient> liveClientFactory, Func<IOpenClient> demoClientFactory)
        {
            _liveClientFactory = liveClientFactory ?? throw new ArgumentNullException(nameof(liveClientFactory));
            _demoClientFactory = demoClientFactory ?? throw new ArgumentNullException(nameof(demoClientFactory));
        }

        public bool IsConnected { get; private set; }

        public IOpenClient LiveClient { get; private set; }

        public IOpenClient DemoClient { get; private set; }

        public async Task Connect()
        {
            IOpenClient liveClient = null;
            IOpenClient demoClient = null;

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

            LiveClient = liveClient;
            DemoClient = demoClient;

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

            using var liveDisposable = LiveClient.OfType<ProtoOAApplicationAuthRes>()
                .Subscribe(response => isLiveClientAppAuthorized = true);
            using var demoDisposable = DemoClient.OfType<ProtoOAApplicationAuthRes>()
                .Subscribe(response => isDemoClientAppAuthorized = true);

            await LiveClient.SendMessage(authRequest, ProtoOAPayloadType.ProtoOaApplicationAuthReq);
            await DemoClient.SendMessage(authRequest, ProtoOAPayloadType.ProtoOaApplicationAuthReq);

            var waitStartTime = DateTime.Now;

            while (!isLiveClientAppAuthorized && !isDemoClientAppAuthorized && DateTime.Now - waitStartTime < TimeSpan.FromSeconds(5))
            {
                await Task.Delay(1000);
            }

            if (!isLiveClientAppAuthorized || !isDemoClientAppAuthorized)
            {
                throw new TimeoutException();
            }
        }

        public void Dispose()
        {
            if (LiveClient is not null) LiveClient.Dispose();
            if (DemoClient is not null) DemoClient.Dispose();
        }

        private void VerifyConnection()
        {
            if (IsConnected is not true) throw new InvalidOperationException("The API service is not connected yet, please connect the service before calling this method");
        }
    }
}