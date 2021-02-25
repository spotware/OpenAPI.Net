using System;
using System.Collections.Generic;
using System.Web;

namespace OpenAPI.Net.Auth
{
    public class AuthCode
    {
        public AuthCode(string code, App app, Scope scope, Mode mode)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            App = app ?? throw new ArgumentNullException(nameof(app));
            Scope = scope;
            Mode = mode;
        }

        public AuthCode(Uri uri, App app, Scope scope, Mode mode)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var code = HttpUtility.ParseQueryString(uri.Query).Get("code");

            if (string.IsNullOrEmpty(code))
            {
                throw new KeyNotFoundException($"The authentication code parameter not found in provided URL query," +
                    $" the provided URL query is: {uri.Query}");
            }

            Code = code;
            App = app ?? throw new ArgumentNullException(nameof(app));
            Scope = scope;
            Mode = mode;
        }

        public string Code { get; }
        public App App { get; }
        public Scope Scope { get; }
        public Mode Mode { get; }
    }
}