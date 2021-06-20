using ASP.NET.Demo.Hubs;
using ASP.NET.Demo.Models;
using ASP.NET.Demo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAPI.Net;
using OpenAPI.Net.Helpers;
using System;

namespace ASP.NET.Demo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            var appSettingsDev = new ConfigurationBuilder().AddJsonFile("appsettings-dev.json").Build();

            var apiCredentials = appSettingsDev.GetSection("ApiCredentials").Get<ApiCredentials>();

            services.AddSingleton(apiCredentials);

            OpenClient liveClientFactory() => new(ApiInfo.LiveHost, ApiInfo.Port, TimeSpan.FromSeconds(10));
            OpenClient demoClientFactory() => new(ApiInfo.DemoHost, ApiInfo.Port, TimeSpan.FromSeconds(10));

            var apiService = new OpenApiService(liveClientFactory, demoClientFactory, apiCredentials);

            services.AddSingleton<IOpenApiService>(apiService);
            services.AddSingleton<ITradingAccountsService>(new TradingAccountsService(apiService));

            services.AddHostedService<ConnectApiHostedService>();

            services.AddSignalR();
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