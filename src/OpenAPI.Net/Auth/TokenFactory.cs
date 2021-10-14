using OpenAPI.Net.Helpers;
using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenAPI.Net.Auth
{
    public static class TokenFactory
    {
        public static async Task<Token> GetToken(string authCode, App app, string authUri = ApiInfo.AuthUrl)
        {
            using var client = new HttpClient();

            var query = new NameValueCollection
            {
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "redirect_uri", app.RedirectUri },
                { "client_id", app.ClientId },
                { "client_secret", app.Secret }
            }.ToQueryString();

            var uri = new Uri($"{authUri}token{query}");

            using var response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var token = await DeserializeToken(response.Content);

                if (string.IsNullOrWhiteSpace(token.ErrorCode) is false)
                {
                    throw new HttpRequestException($"{token.ErrorCode}, {token.ErrorDescription}");
                }

                return token;
            }

            throw new HttpRequestException($"{response.StatusCode}, The HTTP request for getting access token was not successful");
        }

        private static async Task<Token> DeserializeToken(HttpContent content)
        {
            var contentAsString = await content.ReadAsStringAsync();

            var token = JsonSerializer.Deserialize<Token>(contentAsString);

            if (string.IsNullOrWhiteSpace(token.ErrorCode))
            {
                if (string.IsNullOrWhiteSpace(token.AccessToken))
                    throw new JsonException("Access token is not deserialized and is null");
                if (string.IsNullOrWhiteSpace(token.RefreshToken))
                    throw new JsonException("Refresh token is not deserialized and is null");
            }

            return token;
        }
    }
}