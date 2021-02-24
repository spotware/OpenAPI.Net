using System;
using Xunit;
using OpenAPI.Net.Helpers;
using System.Diagnostics;

namespace OpenAPI.Net.Tests
{
    public class OpenClientTests
    {
        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void ConnectTest(string host, int port)
        {
            var client = new OpenClient(host, port, OnListenerException);

            await client.Connect();
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void DisposeTest(string host, int port)
        {
            var client = new OpenClient(host, port, OnListenerException);

            await client.Connect();

            await client.DisposeAsync();
        }

        [Theory]
        [InlineData(ApiInfo.LiveHost, ApiInfo.Port)]
        [InlineData(ApiInfo.DemoHost, ApiInfo.Port)]
        public async void ConnectDisposedTest(string host, int port)
        {
            var client = new OpenClient(host, port, OnListenerException);

            await client.Connect();

            await client.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(client.Connect);
        }

        private void OnListenerException(Exception exception) => Debug.WriteLine(exception);
    }
}
