using ASP.NET.Demo.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Hubs
{
    public class TradingAccountHub : Hub
    {
        private readonly ITradingAccountsService _tradingAccountsService;

        public TradingAccountHub(ITradingAccountsService tradingAccountsService)
        {
            _tradingAccountsService = tradingAccountsService;
        }

        public async Task GetSymbols(string accountLogin)
        {
            var accountModel = await _tradingAccountsService.GetAccountModelByLogin(Convert.ToInt64(accountLogin));

            await Clients.Caller.SendAsync("ReceiveSymbols", new { accountLogin, Symbols = accountModel.Symbols.Select(iSymbol => new { iSymbol.Name, iSymbol.Bid, iSymbol.Ask, iSymbol.Id }) });
        }
    }
}