using ASP.NET.Demo.Services;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenAPI.Net.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Pages
{
    public class ClientAreaModel : PageModel
    {
        private readonly IOpenApiService _apiService;
        private readonly ITradingAccountsService _accountsService;

        public ClientAreaModel(IOpenApiService apiService, ITradingAccountsService accountsService)
        {
            _apiService = apiService;
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

            SubscribeToErrors(_apiService.LiveObservable);
            SubscribeToErrors(_apiService.DemoObservable);

            Accounts.AddRange(await _accountsService.GetAccounts(Token.AccessToken));

            var selectedAccount = Accounts.FirstOrDefault();

            if (selectedAccount is not null)
            {
                AccountLogin = selectedAccount.TraderLogin;
            }
        }

        private void SubscribeToErrors(IObservable<IMessage> observable)
        {
            if (observable is null) throw new ArgumentNullException(nameof(observable));

            observable.Subscribe(_ => { }, OnError);
            observable.OfType<ProtoErrorRes>().Subscribe(OnErrorRes);
            observable.OfType<ProtoOAErrorRes>().Subscribe(OnOaErrorRes);
            observable.OfType<ProtoOAOrderErrorEvent>().Subscribe(OnOrderErrorRes);
        }

        private async void OnError(Exception exception)
        {
        }

        private async void OnOrderErrorRes(ProtoOAOrderErrorEvent error)
        {
        }

        private async void OnOaErrorRes(ProtoOAErrorRes error)
        {
        }

        private async void OnErrorRes(ProtoErrorRes error)
        {
        }
    }
}