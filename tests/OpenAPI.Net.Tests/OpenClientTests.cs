using System;
using Xunit;
using OpenAPI.Net.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using Google.Protobuf;

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
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void AppAuthTest(string host, int port)
        {
            var client = new OpenClient(host, port, TimeSpan.FromSeconds(10));

            await client.Connect();

            var isResponseReceived = false;

            client.Where(message => message is ProtoOAApplicationAuthRes).Subscribe(message => isResponseReceived = true);

            var appAuhRequest = new ProtoOAApplicationAuthReq
            {
                ClientId = "699_9UIX3RJWkl3BwGfKi30xzfiyCaMkEA1FLKD020gy57i4e3XplL",
                ClientSecret = "dfJVd3Ud1HkLcQJaLPx5fmEqR8iUkmLYeCBikQUa6J3bJH2Jce"
            };

            await client.SendMessage(appAuhRequest, ProtoOAPayloadType.ProtoOaApplicationAuthReq);

            await Task.Delay(2000);

            await client.DisposeAsync();

            Assert.True(isResponseReceived);
        }
    }
}
