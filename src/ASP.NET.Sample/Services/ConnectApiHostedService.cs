using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Samples.Shared.Services;

namespace ASP.NET.Sample.Services
{
    public class ConnectApiHostedService : IHostedService
    {
        private readonly IOpenApiService _apiService;

        public ConnectApiHostedService(IOpenApiService apiService)
        {
            _apiService = apiService;
        }

        public Task StartAsync(CancellationToken cancellationToken) => _apiService.Connect();

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}