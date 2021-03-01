namespace Trading.UI.Demo.Models
{
    public class MarketOrderModel : OrderModel
    {
        public double MarketRangeInPips { get; set; }

        public bool IsMarketRange { get; set; }
    }
}