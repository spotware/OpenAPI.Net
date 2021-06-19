using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Services
{
    public class ConnectApiHostedService : IHostedService
    {
        private readonly IApiService _apiService;

        public ConnectApiHostedService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public Task StartAsync(CancellationToken cancellationToken) => _apiService.Connect();

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}