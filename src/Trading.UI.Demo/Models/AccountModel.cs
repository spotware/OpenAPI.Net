using System;

namespace Trading.UI.Demo.Models
{
    public class AccountModel
    {
        public long Id { get; init; }

        public bool IsLive { get; init; }

        public SymbolModel[] Symbols { get; init; }

        public ProtoOATrader Trader { get; init; }

        public DateTimeOffset RegistrationTime { get; init; }
    }
}