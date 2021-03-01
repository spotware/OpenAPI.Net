using OpenAPI.Net;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using auth = OpenAPI.Net.Auth;

namespace Trading.UI.Demo.Services
{
    public interface IApiService
    {
        IOpenClient LiveClient { get; }
        IOpenClient DemoClient { get; }

        Task AuthorizeApp(auth.App app);
    }

    public class ApiService : IApiService
    {
        public ApiService(IOpenClient liveClient, IOpenClient demoClient)
        {
            LiveClient = liveClient ?? throw new ArgumentNullException(nameof(liveClient));

            DemoClient = demoClient ?? throw new ArgumentNullException(nameof(demoClient));
        }

        public IOpenClient LiveClient { get; }
        public IOpenClient DemoClient { get; }

        public async Task AuthorizeApp(auth.App app)
        {
            var authRequest = new ProtoOAApplicationAuthReq
            {
                ClientId = app.ClientId,
                ClientSecret = app.Secret,
            };

            bool isLiveClientAppAuthorized = false;
            bool isDemoClientAppAuthorized = false;

            using var liveDisposable = LiveClient.OfType<ProtoOAApplicationAuthRes>()
                .Subscribe(response => isLiveClientAppAuthorized = true);
            using var demoDisposable = LiveClient.OfType<ProtoOAApplicationAuthRes>()
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
    }
}