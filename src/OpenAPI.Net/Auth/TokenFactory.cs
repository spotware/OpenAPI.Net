using OpenAPI.Net.Helpers;
using RestSharp;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenAPI.Net.Auth
{
    public static class TokenFactory
    {
        public static async Task<Token> GetToken(string authCode, App app, string authUri = ApiInfo.AuthUrl)
        {
            var client = new RestClient(authUri);

            var request = GetTokenRequest(authCode, app);

            var response = await client.ExecuteGetAsync(request).ConfigureAwait(false);

            var token = DeserializeToken(response);

            return token;
        }

        private static RestRequest GetTokenRequest(string authCode, App app)
        {
            var request = new RestRequest("token");

            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", authCode);
            request.AddParameter("redirect_uri", app.RedirectUri);
            request.AddParameter("client_id", app.ClientId);
            request.AddParameter("client_secret", app.Secret);

            return request;
        }

        private static Token DeserializeToken(IRestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK && response.ResponseStatus == ResponseStatus.Completed)
            {
                var token = JsonSerializer.Deserialize<Token>(response.Content);

                if (string.IsNullOrWhiteSpace(token.ErrorCode))
                {
                    if (string.IsNullOrWhiteSpace(token.AccessToken))
                        throw new JsonException("Access token is not deserialized and is null");
                    if (string.IsNullOrWhiteSpace(token.RefreshToken))
                        throw new JsonException("Refresh token is not deserialized and is null");
                }

                return token;
            }
            else
            {
                throw new WebException(response.ErrorMessage, response.ErrorException);
            }
        }
    }
}