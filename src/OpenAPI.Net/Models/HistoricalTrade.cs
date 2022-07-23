using System;

namespace OpenAPI.Net.Models
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

        public double Swap { get; set; }

        public double Commission { get; set; }

        public double GrossProfit { get; set; }

        public double ClosedBalance { get; set; }

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
        public HistoricalTrade(ProtoOADeal deal)
        {
            Id = deal.DealId;
            SymbolId = deal.SymbolId;
            OrderId = deal.OrderId;
            PositionId = deal.PositionId;
            Volume = deal.Volume;
            FilledVolume = deal.FilledVolume;
            Direction = deal.TradeSide.ToString();
            Status = deal.DealStatus.ToString();
            Commission = deal.Commission;
            ExecutionPrice = deal.ExecutionPrice;
            MarginRate = deal.MarginRate;
            BaseToUsdConversionRate = deal.BaseToUsdConversionRate;
            CreationTime = DateTimeOffset.FromUnixTimeMilliseconds(deal.CreateTimestamp);
            ExecutionTime = DateTimeOffset.FromUnixTimeMilliseconds(deal.ExecutionTimestamp);
            LastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(deal.UtcLastUpdateTimestamp);

            if (deal.ClosePositionDetail != null)
            {
                IsClosing = true;
                ClosedVolume = deal.ClosePositionDetail.ClosedVolume;
                GrossProfit = deal.ClosePositionDetail.GrossProfit;
                Swap = deal.ClosePositionDetail.Swap;
                ClosedBalance = deal.ClosePositionDetail.Balance;
                QuoteToDepositConversionRate = deal.ClosePositionDetail.QuoteToDepositConversionRate;
            }
            else
            {
                IsClosing = false;
            }
        }
    }
}