using ASP.NET.Sample.Hubs;
using Samples.Shared.Models;
using Samples.Shared.Services;
using ASP.NET.Sample.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAPI.Net;
using OpenAPI.Net.Helpers;
using System;
using System.Text.Json;

namespace ASP.NET.Sample
{
    public class Startup
    {
        private readonly ApiCredentials _apiCredentials;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var appSettingsDev = new ConfigurationBuilder().AddJsonFile("appsettings-dev.json").Build();

            _apiCredentials = appSettingsDev.GetSection("ApiCredentials").Get<ApiCredentials>();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            OpenClient liveClientFactory() => new(ApiInfo.LiveHost, ApiInfo.Port, TimeSpan.FromSeconds(10));
            OpenClient demoClientFactory() => new(ApiInfo.DemoHost, ApiInfo.Port, TimeSpan.FromSeconds(10));

            services.AddSingleton(_apiCredentials);

            var apiService = new OpenApiService(liveClientFactory, demoClientFactory);

            services.AddSingleton<IOpenApiService>(apiService);
            services.AddSingleton<ITradingAccountsService>(new TradingAccountsService(apiService));

            services.AddHostedService<ConnectApiHostedService>();

            services.AddSignalR(hubOptions => hubOptions.EnableDetailedErrors = true).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<TradingAccountHub>("/tradingAccountHub");
            });
        }
    }
}