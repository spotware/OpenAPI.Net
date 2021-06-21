using ASP.NET.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenAPI.Net.Auth;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Pages
{
    public class ClientAreaModel : PageModel
    {
        private readonly ITradingAccountsService _accountsService;

        public ClientAreaModel(ITradingAccountsService accountsService)
        {
            _accountsService = accountsService;
        }

        public Token Token { get; private set; }

        public List<ProtoOACtidTraderAccount> Accounts { get; } = new();

        [BindProperty(SupportsGet = true)]
        public long AccountLogin { get; set; }

        public async Task OnGetAsync()
        {
            if (!TempData.TryGetValue("Token", out var tokenJson)) return;

            TempData.Remove("Token");

            Token = JsonSerializer.Deserialize<Token>(tokenJson.ToString());

            Accounts.AddRange(await _accountsService.GetAccounts(Token.AccessToken));

            var selectedAccount = Accounts.FirstOrDefault();

            if (selectedAccount is not null)
            {
                AccountLogin = selectedAccount.TraderLogin;
            }
        }
    }
}