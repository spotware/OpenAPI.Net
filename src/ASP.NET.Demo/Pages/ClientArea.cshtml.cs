using ASP.NET.Demo.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenAPI.Net.Auth;
using System.Text.Json;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Pages
{
    public class ClientAreaModel : PageModel
    {
        private readonly ApiService _apiService;

        public ClientAreaModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public Token Token { get; set; }

        public ProtoOACtidTraderAccount[] Accounts { get; set; }

        public async Task OnGetAsync()
        {
            if (!TempData.TryGetValue("Token", out var tokenJson)) return;

            Token = JsonSerializer.Deserialize<Token>(tokenJson.ToString());

            await _apiService.Connect();

            Accounts = await _apiService.GetAccountsList(Token.AccessToken);
        }
    }
}