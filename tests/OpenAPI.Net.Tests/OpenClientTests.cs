using OpenAPI.Net.Helpers;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAPI.Net.Tests
{
    public class OpenClientTests
    {
        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void ConnectTest(string host, int port)
        {
            var client = new OpenClient(host, port, TimeSpan.FromSeconds(10));

            await client.Connect();
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void DisposeTest(string host, int port)
        {
            var client = new OpenClient(host, port, TimeSpan.FromSeconds(1));

            Exception exception = null;

            client.Subscribe(message => { }, ex => exception = ex);

            await client.Connect();

            await Task.Delay(5000);

            await client.DisposeAsync();

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void ConnectDisposedTest(string host, int port)
        {
            var client = new OpenClient(host, port, TimeSpan.FromSeconds(10));

            await client.Connect();

            await client.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(client.Connect);
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port, "", "")]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port, "", "")]
        public async void AppAuthTest(string host, int port, string appId, string appSecret)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentException($"'{nameof(appId)}' cannot be null or whitespace", nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(appSecret))
            {
                throw new ArgumentException($"'{nameof(appSecret)}' cannot be null or whitespace", nameof(appSecret));
            }

            var client = new OpenClient(host, port, TimeSpan.FromSeconds(10));

            await client.Connect();

            var isResponseReceived = false;

            Exception exception = null;

            client.OfType<ProtoOAApplicationAuthRes>().Subscribe(message => isResponseReceived = true, ex => exception = ex);
            
            var appAuhRequest = new ProtoOAApplicationAuthReq
            {
                ClientId = appId,
                ClientSecret = appSecret
            };

            await client.SendMessage(appAuhRequest, ProtoOAPayloadType.ProtoOaApplicationAuthReq);

            await Task.Delay(2000);

            await client.DisposeAsync();

            Assert.True(isResponseReceived && exception is null);
        }
    }
}