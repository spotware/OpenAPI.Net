using System;

namespace Samples.Shared.Models
{
    public class NewPendingOrderRequest
    {
        public long AccountLogin { get; set; }

        public double Price { get; set; }

        public long SymbolId { get; set; }

        public double Volume { get; set; }

        public string Direction { get; set; }

        public string Comment { get; set; }

        public bool HasExpiry { get; set; }

        public DateTimeOffset Expiry { get; set; }

        public string Type { get; set; }

        public long LimitRange { get; set; }

        public bool HasStopLoss { get; set; }

        public long StopLoss { get; set; }

        public bool HasTrailingStop { get; set; }

        public bool HasTakeProfit { get; set; }

        public long TakeProfit { get; set; }
    }
}