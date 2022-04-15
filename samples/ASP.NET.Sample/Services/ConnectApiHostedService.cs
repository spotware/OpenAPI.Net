using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Samples.Shared.Services;
using Samples.Shared.Models;

namespace ASP.NET.Sample.Services
{
    public class ConnectApiHostedService : IHostedService
    {
        private readonly IOpenApiService _apiService;
        private readonly ApiCredentials _apiCredentials;

        public ConnectApiHostedService(IOpenApiService apiService, ApiCredentials apiCredentials)
        {
            _apiService = apiService;
            _apiCredentials = apiCredentials;
        }

        public Task StartAsync(CancellationToken cancellationToken) => _apiService.Connect(_apiCredentials);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}