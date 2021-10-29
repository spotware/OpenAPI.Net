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
        /// <summary>
        /// This method returns an API access token
        /// It creates an instance of HttpClient and then disposes that instance after sending the request
        /// If you are using HttpClientFactory then user the other overload of GetToken method that you can pass an HttpClient
        /// </summary>
        /// <param name="authCode">Authentication code</param>
        /// <param name="app">API application</param>
        /// <param name="authUri">Optional, if you don't pass a value for this parameter it will use the ApiInfo.AuthUrl</param>
        /// <exception cref="ArgumentNullException">When any of the method parameters were null.</exception>
        /// <exception cref="HttpRequestException">If HttpRequest failed</exception>
        /// <returns>Token</returns>
        public static async Task<Token> GetToken(string authCode, App app, string authUri = ApiInfo.AuthUrl)
        {
            using var client = new HttpClient();

            return await GetToken(authCode, app, client, authUri);
        }

        /// <summary>
        /// This method returns an API access token
        /// Use this overload of GetToken method if you have an HttpClientFactory
        /// </summary>
        /// <param name="authCode">Authentication code</param>
        /// <param name="app">API application</param>
        /// <param name="client">The HttpClient instance that will be used for sending request</param>
        /// <param name="authUri">Optional, if you don't pass a value for this parameter it will use the ApiInfo.AuthUrl</param>
        /// <exception cref="ArgumentNullException">When any of the method parameters were null.</exception>
        /// <exception cref="HttpRequestException">If HttpRequest failed</exception>
        /// <returns>Token</returns>
        public static async Task<Token> GetToken(string authCode, App app, HttpClient client, string authUri = ApiInfo.AuthUrl)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                throw new ArgumentException($"'{nameof(authCode)}' cannot be null or empty.", nameof(authCode));
            }

            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (authUri is null)
            {
                throw new ArgumentNullException(nameof(authUri));
            }

            var uri = GetUri(authCode, app, authUri);

            using var response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var contentAsString = await response.Content.ReadAsStringAsync();

                var token = DeserializeToken(contentAsString);

                if (string.IsNullOrWhiteSpace(token.ErrorCode) is false)
                {
                    throw new HttpRequestException($"{token.ErrorCode}, {token.ErrorDescription}");
                }

                return token;
            }

            throw new HttpRequestException($"{response.StatusCode}, The HTTP request for getting access token was not successful");
        }

        public static Uri GetUri(string authCode, App app, string authUri = ApiInfo.AuthUrl)
        {
            var query = new NameValueCollection
            {
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "redirect_uri", app.RedirectUri },
                { "client_id", app.ClientId },
                { "client_secret", app.Secret }
            }.ToQueryString();

            return new Uri($"{authUri}token{query}");
        }

        public static Token DeserializeToken(string json)
        {
            var token = JsonSerializer.Deserialize<Token>(json);

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