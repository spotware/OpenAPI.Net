using System;

namespace ASP.NET.Demo.Models
{
    public class Transaction
    {
        public long Id { get; set; }

        public ProtoOAChangeBalanceType Type { get; set; }

        public double Balance { get; set; }

        public double Delta { get; set; }

        public long BalanceVersion { get; set; }

        public long Equity { get; set; }

        public DateTimeOffset Time { get; set; }

        public string Note { get; set; }
    }
}