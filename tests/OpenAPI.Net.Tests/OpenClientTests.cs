using System;
using Xunit;

namespace OpenAPI.Net.Tests
{
    public class OpenClientTests
    {
        private const string LiveHost = "live.ctraderapi.com";
        private const string DemoHost = "demo.ctraderapi.com";

        private const int Port = 5035;

        [Theory]
        [InlineData(LiveHost, Port)]
        [InlineData(DemoHost, Port)]
        public async void ConnectTest(string host, int port)
        {
            var client = new OpenClient(host, port);

            await client.Connect();
        }

        [Theory]
        [InlineData(LiveHost, Port)]
        [InlineData(DemoHost, Port)]
        public async void DisposeTest(string host, int port)
        {
            var client = new OpenClient(host, port);

            await client.Connect();

            client.Dispose();
        }
    }
}
