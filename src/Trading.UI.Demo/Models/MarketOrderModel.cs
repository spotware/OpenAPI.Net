namespace Trading.UI.Demo.Models
{
    public class MarketOrderModel : OrderModel
    {
        private double _marketRangeInPips = 10;
        private bool _isMarketRange;

        public MarketOrderModel()
        {
        }

        public MarketOrderModel(PositionModel position)
        {
            Id = position.Id;

            TradeSide = position.TradeData.TradeSide;

            Symbol = position.Symbol;

            if (position.StopLossInPips.HasValue)
            {
                IsStopLossEnabled = true;
                StopLossInPips = position.StopLossInPips.Value;
            }

            if (position.TakeProfitInPips.HasValue)
            {
                IsTakeProfitEnabled = true;
                TakeProfitInPips = position.TakeProfitInPips.Value;
            }

            Volume = position.TradeData.Volume;

            Comment = position.TradeData.Comment;

            Label = position.TradeData.Label;

            Price = position.Price;
        }

        public double MarketRangeInPips { get => _marketRangeInPips; set => SetProperty(ref _marketRangeInPips, value); }

        public bool IsMarketRange { get => _isMarketRange; set => SetProperty(ref _isMarketRange, value); }
    }
}