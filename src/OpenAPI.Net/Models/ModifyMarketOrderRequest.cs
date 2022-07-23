namespace OpenAPI.Net.Models
{
    public class ModifyMarketOrderRequest
    {
        public long AccountLogin { get; set; }

        public long Id { get; set; }

        public double Volume { get; set; }

        public string Direction { get; set; }

        public bool HasStopLoss { get; set; }

        public long StopLoss { get; set; }

        public bool HasTrailingStop { get; set; }

        public bool HasTakeProfit { get; set; }

        public long TakeProfit { get; set; }
    }
}