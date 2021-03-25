using System;
using System.Collections.Generic;

namespace Trading.UI.Demo.Models
{
    public class AccountModel
    {
        public long Id { get; init; }

        public bool IsLive { get; init; }

        public SymbolModel[] Symbols { get; init; }

        public ProtoOATrader Trader { get; init; }

        public DateTimeOffset RegistrationTime { get; init; }

        public ProtoOAAsset DepositAsset { get; init; }

        public string Currency => DepositAsset.Name;

        public IReadOnlyCollection<ProtoOAAsset> Assets { get; init; }

        public double Balance { get; set; }

        public double Equity { get; set; }

        public double MarginUsed { get; set; }

        public double FreeMargin { get; set; }

        public double MarginLevel { get; set; }

        public double UnrealizedGrossProfit { get; set; }

        public double UnrealizedNetProfit { get; set; }
    }
}