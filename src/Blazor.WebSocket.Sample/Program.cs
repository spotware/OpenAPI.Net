using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAPI.Net;
using OpenAPI.Net.Helpers;
using Samples.Shared.Models;
using Samples.Shared.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.WebSocket.Sample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            OpenClient liveClientFactory() => new(ApiInfo.LiveHost, ApiInfo.Port, TimeSpan.FromSeconds(10), useWebSocket: true);
            OpenClient demoClientFactory() => new(ApiInfo.DemoHost, ApiInfo.Port, TimeSpan.FromSeconds(10), useWebSocket: true);

            var apiService = new OpenApiService(liveClientFactory, demoClientFactory);

            builder.Services.AddSingleton<IOpenApiService>(apiService);
            //builder.Services.AddSingleton<ITradingAccountsService>(new TradingAccountsService(apiService));

            await builder.Build().RunAsync();
        }
    }
}