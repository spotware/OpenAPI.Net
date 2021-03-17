namespace Trading.UI.Demo.Models
{
    public class MarketOrderModel : OrderModel
    {
        private double _marketRangeInPips;
        private bool _isMarketRange;

        public double MarketRangeInPips { get => _marketRangeInPips; set => SetProperty(ref _marketRangeInPips, value); }

        public bool IsMarketRange { get => _isMarketRange; set => SetProperty(ref _isMarketRange, value); }
    }
}