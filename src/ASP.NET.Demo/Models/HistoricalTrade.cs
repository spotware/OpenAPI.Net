using System;

namespace ASP.NET.Demo.Models
{
    public class HistoricalTrade
    {
        public long Id { get; set; }

        public string SymbolName { get; set; }

        public long SymbolId { get; set; }

        public long OrderId { get; set; }

        public long PositionId { get; set; }

        public double Volume { get; set; }

        public double FilledVolume { get; set; }

        public double ClosedVolume { get; set; }

        public long Swap { get; set; }

        public long Commission { get; set; }

        public long GrossProfit { get; set; }

        public long ClosedBalance { get; set; }

        public double ExecutionPrice { get; set; }

        public double BaseToUsdConversionRate { get; set; }

        public double MarginRate { get; set; }

        public double QuoteToDepositConversionRate { get; set; }

        public DateTimeOffset LastUpdateTime { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public DateTimeOffset ExecutionTime { get; set; }

        public string Direction { get; set; }

        public string Status { get; set; }

        public bool IsClosing { get; set; }
    }
}