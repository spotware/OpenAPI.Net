using ASP.NET.Demo.Models;
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
        private readonly IApiService _apiService;

        public ClientAreaModel(IApiService apiService)
        {
            _apiService = apiService;
        }

        public Token Token { get; private set; }

        public List<ProtoOACtidTraderAccount> Accounts => _apiService.Accounts;

        [BindProperty(SupportsGet = true)]
        public long AccountLogin { get; set; }

        public AccountModel AccountModel { get; set; }

        public async Task OnGetAsync()
        {
            if (!TempData.TryGetValue("Token", out var tokenJson)) return;

            TempData.Remove("Token");

            Token = JsonSerializer.Deserialize<Token>(tokenJson.ToString());

            await _apiService.Connect();

            SubscribeToErrors(_apiService.LiveObservable);
            SubscribeToErrors(_apiService.DemoObservable);

            await _apiService.GetAccountsList(Token.AccessToken);

            foreach (var account in Accounts)
            {
                await _apiService.AuthorizeAccount((long)account.CtidTraderAccountId, account.IsLive, Token.AccessToken);
            }

            var selectedAccount = Accounts.FirstOrDefault();

            if (selectedAccount is not null)
            {
                AccountLogin = selectedAccount.TraderLogin;

                await OnGetAccountChanged();
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

        public async Task OnGetAccountChanged()
        {
            var account = _apiService.Accounts.FirstOrDefault(iAccount => iAccount.TraderLogin == AccountLogin);

            if (account is null || _apiService.AccountModels.ContainsKey(account.CtidTraderAccountId)) return;

            await _apiService.CreateAccountModel(account);

            AccountModel = _apiService.AccountModels[account.CtidTraderAccountId];
        }

        public JsonResult OnGetSymbols()
        {
            var account = _apiService.Accounts.FirstOrDefault(iAccount => iAccount.TraderLogin == AccountLogin);

            if (account is null || !_apiService.AccountModels.TryGetValue(account.CtidTraderAccountId, out var accountModel) || accountModel.Symbols is null) return new JsonResult(null);

            var symbols = new JsonResult(accountModel.Symbols.Select(iSymbol => new { iSymbol.Name, iSymbol.Bid, iSymbol.Ask, iSymbol.Id }));

            return symbols;
        }
    }
}