using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAPI.Net.Auth
{
    public class Token
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }

        [JsonConverter(typeof(TokenExpiryConverter))]
        [JsonPropertyName("expiresIn")]
        public DateTimeOffset ExpiresIn { get; set; }

        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; }

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("description")]
        public string ErrorDescription { get; set; }

        private class TokenExpiryConverter : JsonConverter<DateTimeOffset>
        {
            public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTimeOffset.UtcNow.AddSeconds(reader.GetInt64());
            }

            public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(Convert.ToInt64((value - DateTimeOffset.UtcNow).TotalSeconds));
            }
        }
    }
}