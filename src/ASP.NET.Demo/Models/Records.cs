using OpenAPI.Net.Helpers;
using System;

namespace ASP.NET.Demo.Models
{
    public record Position
    {
        public long Id { get; init; }
        public string Direction { get; init; }
        public double Volume { get; init; }
        public double Price { get; init; }
        public double StopLoss { get; init; }
        public double TakeProfit { get; init; }
        public double Commission { get; init; }
        public double Swap { get; init; }
        public double Margin { get; init; }
        public double Pips { get; init; }
        public double NetProfit { get; init; }
        public double GrossProfit { get; init; }
        public DateTimeOffset OpenTime { get; init; }
        public string Symbol { get; init; }
        public string Label { get; init; }
        public string Comment { get; init; }

        public static Position FromMarketOrder(MarketOrderModel marketOrder) => new()
        {
            Id = marketOrder.Id,
            Direction = marketOrder.TradeSide.ToString(),
            Volume = MonetaryConverter.FromMonetary(marketOrder.Volume),
            Price = marketOrder.Price,
            StopLoss = marketOrder.StopLossInPrice,
            TakeProfit = marketOrder.TakeProfitInPrice,
            Commission = marketOrder.DoubleCommissionMonetary,
            Swap = marketOrder.SwapMonetary,
            Margin = MonetaryConverter.FromMonetary(marketOrder.UsedMargin),
            Pips = marketOrder.Pips,
            GrossProfit = marketOrder.GrossProfit,
            NetProfit = marketOrder.NetProfit,
            Symbol = marketOrder.Symbol.Name,
            Comment = marketOrder.Comment,
            Label = marketOrder.Label,
            OpenTime = marketOrder.OpenTime
        };
    }

    public record SymbolQuote(long Id, double Bid, double Ask);
}