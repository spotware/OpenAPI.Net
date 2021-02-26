using OpenAPI.Net.Auth;

namespace OpenAPI.Net.Helpers
{
    public static class ApiInfo
    {
        public const string LiveHost = "live.ctraderapi.com";
        public const string DemoHost = "demo.ctraderapi.com";

        public const int Port = 5035;

        public const string AuthUrl = "https://connect.spotware.com/apps/";

        public static string GetHost(Mode mode) => mode == Mode.Live ? LiveHost : DemoHost;
    }
}