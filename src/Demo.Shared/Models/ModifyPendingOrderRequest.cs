using System;

namespace Samples.Shared.Models
{
    public class ModifyPendingOrderRequest
    {
        public long AccountLogin { get; set; }

        public double Price { get; set; }

        public long Id { get; set; }

        public double Volume { get; set; }

        public bool HasExpiry { get; set; }

        public DateTimeOffset Expiry { get; set; }

        public long LimitRange { get; set; }

        public bool HasStopLoss { get; set; }

        public long StopLoss { get; set; }

        public bool HasTrailingStop { get; set; }

        public bool HasTakeProfit { get; set; }

        public long TakeProfit { get; set; }
    }
}