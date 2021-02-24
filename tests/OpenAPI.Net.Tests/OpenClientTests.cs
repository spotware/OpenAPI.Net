using System;
using Xunit;
using OpenAPI.Net.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenAPI.Net.Tests
{
    public class OpenClientTests
    {
        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void ConnectTest(string host, int port)
        {
            var client = new OpenClient(host, port, OnListenerException, TimeSpan.FromSeconds(1));

            await client.Connect();
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void DisposeTest(string host, int port)
        {
            var client = new OpenClient(host, port, OnListenerException, TimeSpan.FromSeconds(1));

            await client.Connect();

            await Task.Delay(5000);

            await client.DisposeAsync();

            Assert.True(_exceptionNumber == 0);
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void ConnectDisposedTest(string host, int port)
        {
            var client = new OpenClient(host, port, OnListenerException, TimeSpan.FromSeconds(1));

            await client.Connect();

            await client.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(client.Connect);
        }

        private int _exceptionNumber;
        private void OnListenerException(Exception exception) => _exceptionNumber++;
    }
}
