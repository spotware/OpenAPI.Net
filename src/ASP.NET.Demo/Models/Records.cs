using OpenAPI.Net.Helpers;
using System;

namespace ASP.NET.Demo.Models
{
    public record Order
    {
        public long Id { get; init; }
        public string Direction { get; init; }
        public double Volume { get; init; }
        public double Price { get; init; }
        public double StopLoss { get; init; }
        public double TakeProfit { get; init; }
        public DateTimeOffset OpenTime { get; init; }
        public string Symbol { get; init; }
        public string Label { get; init; }
        public string Comment { get; init; }
    }
    public record Position : Order
    {
        public double Commission { get; init; }
        public double Swap { get; init; }
        public double Margin { get; init; }
        public double Pips { get; init; }
        public double NetProfit { get; init; }
        public double GrossProfit { get; init; }

        public static Position FromModel(MarketOrderModel marketOrder) => new()
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

    public record PendingOrder : Order
    {
        public DateTimeOffset Expiry { get; init; }

        public string Type { get; init; }

        public bool IsFilledOrCanceled { get; init; }

        public bool IsExpiryEnabled { get; set; }

        public static PendingOrder FromModel(PendingOrderModel pendingOrder) => new()
        {
            Id = pendingOrder.Id,
            Direction = pendingOrder.TradeSide.ToString(),
            Volume = MonetaryConverter.FromMonetary(pendingOrder.Volume),
            Price = pendingOrder.Price,
            StopLoss = pendingOrder.StopLossInPrice,
            TakeProfit = pendingOrder.TakeProfitInPrice,
            Symbol = pendingOrder.Symbol.Name,
            Comment = pendingOrder.Comment,
            Label = pendingOrder.Label,
            OpenTime = pendingOrder.OpenTime,
            Expiry = pendingOrder.ExpiryTime,
            IsExpiryEnabled = pendingOrder.IsExpiryEnabled,
            Type = pendingOrder.Type.ToString(),
            IsFilledOrCanceled = pendingOrder.IsFilledOrCanceled
        };
    }

    public record SymbolQuote(long Id, double Bid, double Ask);

    public record Error(string Message, string Type);
}