using System;
using Xunit;

namespace OpenAPI.Net.Tests
{
    public class OpenClientTests
    {
        [Fact]
        public async void ConnectTest()
        {
            var client = new OpenClient();

            await client.Connect();
        }
    }
}
