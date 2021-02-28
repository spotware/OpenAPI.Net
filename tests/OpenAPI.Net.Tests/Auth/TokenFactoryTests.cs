using OpenAPI.Net.Auth;
using Xunit;

namespace OpenAPI.Net.Tests.Auth
{
    public class TokenFactoryTests
    {
        [Theory]
        [InlineData("", "", "", "")]
        public async void GetTokenTest(string appId, string appSecret, string redirectUri, string authCode)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new System.ArgumentException($"'{nameof(appId)}' cannot be null or whitespace", nameof(appId));
            }

            if (string.IsNullOrWhiteSpace(appSecret))
            {
                throw new System.ArgumentException($"'{nameof(appSecret)}' cannot be null or whitespace", nameof(appSecret));
            }

            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                throw new System.ArgumentException($"'{nameof(redirectUri)}' cannot be null or whitespace", nameof(redirectUri));
            }

            if (string.IsNullOrWhiteSpace(authCode))
            {
                throw new System.ArgumentException($"'{nameof(authCode)}' cannot be null or whitespace", nameof(authCode));
            }

            var app = new App(appId, appSecret, redirectUri);

            var token = await TokenFactory.GetToken(authCode, app);

            Assert.NotNull(token);
        }
    }
}