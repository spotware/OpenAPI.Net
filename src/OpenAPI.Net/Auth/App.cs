using OpenAPI.Net.Helpers;
using System;

namespace OpenAPI.Net.Auth
{
    public class App
    {
        public App(string clientId, string secret, string redirectUri)
        {
            ClientId = clientId;
            Secret = secret;
            RedirectUri = redirectUri;
        }

        public string ClientId { get; }
        public string Secret { get; }
        public string RedirectUri { get; }

        public Uri GetAuthUri(Scope scope = Scope.Trading, string authUrl = ApiInfo.AuthUrl)
        {
            var authURIBuilder = new UriBuilder(authUrl);

            authURIBuilder.Path += "auth";

            var scopeString = scope.ToString().ToLowerInvariant();

            authURIBuilder.Query = $"client_id={ClientId}&redirect_uri={RedirectUri}&scope={scopeString}";

            return authURIBuilder.Uri;
        }
    }
}