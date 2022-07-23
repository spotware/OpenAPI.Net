namespace OpenAPI.Net.Models
{
    public class NewMarketOrderRequest
    {
        public long AccountLogin { get; set; }

        public long SymbolId { get; set; }

        public double Volume { get; set; }

        public string Direction { get; set; }

        public string Comment { get; set; }

        public bool IsMarketRange { get; set; }

        public long MarketRange { get; set; }

        public bool HasStopLoss { get; set; }

        public long StopLoss { get; set; }

        public bool HasTrailingStop { get; set; }

        public bool HasTakeProfit { get; set; }

        public long TakeProfit { get; set; }
    }
}