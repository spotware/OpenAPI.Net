using OpenAPI.Net.Auth;
using Xunit;

namespace OpenAPI.Net.Tests.Auth
{
    public class TokenFactoryTests
    {
        [Theory]
        [InlineData("", "", "", "")]
        public async void GetTokenTest(string appId, string appSecret, string redirectUri, string code)
        {
            var app = new App(appId, appSecret, redirectUri);

            var authCode = new AuthCode(code, app, Scope.Trading, Mode.Demo);

            var token = await TokenFactory.GetToken(authCode);

            Assert.NotNull(token);
        }
    }
}