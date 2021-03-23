using System;

namespace Trading.UI.Demo.Models
{
    public class TransactionModel
    {
        public long Id { get; set; }

        public ProtoOAChangeBalanceType Type { get; set; }

        public long Balance { get; set; }

        public long Delta { get; set; }

        public long BalanceVersion { get; set; }

        public long Equity { get; set; }

        public DateTimeOffset Time { get; set; }

        public string Note { get; set; }
    }
}