using ASP.NET.Demo.Models;
using ASP.NET.Demo.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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

        public async IAsyncEnumerable<SymbolQuote> GetSymbolQuotes(string accountLogin, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var accountId = _tradingAccountsService.GetAccountId(Convert.ToInt64(accountLogin));

            var channel = _tradingAccountsService.GetSymbolsQuoteChannel(accountId);

            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (channel.Reader.TryRead(out var quote))
                {
                    yield return quote;
                }
            }
        }

        public void StopSymbolQuotes(string accountLogin)
        {
            var accountId = _tradingAccountsService.GetAccountId(Convert.ToInt64(accountLogin));

            _tradingAccountsService.StopSymbolQuotes(accountId);
        }
    }
}