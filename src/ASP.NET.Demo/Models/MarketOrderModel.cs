using OpenAPI.Net.Helpers;
using System;
using System.Collections.Generic;

namespace ASP.NET.Demo.Models
{
    public class MarketOrderModel : OrderModel, IEquatable<MarketOrderModel>
    {
        private long _swap, _commission;

        public MarketOrderModel()
        {
        }

        public MarketOrderModel(ProtoOAPosition position, SymbolModel symbol) => Update(position, symbol);

        public double MarketRangeInPips { get; set; } = 10;

        public bool IsMarketRange { get; set; }

        public bool GuaranteedStopLoss { get; set; }

        public double MarginRate { get; set; }

        public long MirroringCommission { get; set; }

        public long Swap
        {
            get => _swap;
            set
            {
                _swap = value;

                SwapMonetary = Swap / Math.Pow(10, MoneyDigits);
            }
        }

        public double SwapMonetary { get; set; }

        public long UsedMargin { get; set; }

        public int MoneyDigits { get; set; }

        public long Commission
        {
            get => _commission;
            set
            {
                _commission = value;

                CommissionMonetary = Commission / Math.Pow(10, MoneyDigits);

                DoubleCommissionMonetary = CommissionMonetary * 2;
            }
        }

        public double CommissionMonetary { get; set; }
        public double DoubleCommissionMonetary { get; private set; }

        public ProtoOAPositionStatus Status { get; set; }

        public ProtoOAOrderTriggerMethod StopTriggerMethod { get; set; }

        public double Pips { get; set; }

        public double NetProfit { get; set; }

        public double GrossProfit { get; set; }
        public double BaseSlippagePrice { get; set; }

        public void Update(ProtoOAPosition position, SymbolModel symbol)
        {
            Symbol = symbol;
            TradeSide = position.TradeData.TradeSide;
            Volume = position.TradeData.Volume;
            MoneyDigits = (int)position.MoneyDigits;
            Id = position.PositionId;
            GuaranteedStopLoss = position.GuaranteedStopLoss;
            LastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(position.UtcLastUpdateTimestamp);
            MarginRate = position.MarginRate;
            MirroringCommission = position.MirroringCommission;
            Price = position.Price;
            Status = position.PositionStatus;
            UsedMargin = (long)position.UsedMargin;
            TradeData = position.TradeData;
            StopTriggerMethod = position.StopLossTriggerMethod;
            Swap = position.Swap;
            Commission = position.Commission;
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(position.TradeData.OpenTimestamp);
            Comment = position.TradeData.Comment;
            Label = position.TradeData.Label;

            StopLossInPrice = position.StopLoss;
            TakeProfitInPrice = position.TakeProfit;

            if (position.HasStopLoss)
            {
                IsStopLossEnabled = true;
                StopLossInPrice = position.StopLoss;
                StopLossInPips = Symbol.Data.GetPipsFromPrice(Math.Abs(StopLossInPrice - Price));
            }
            else
            {
                IsStopLossEnabled = false;
                StopLossInPrice = default;
                StopLossInPips = default;

                IsTrailingStopLossEnabled = false;
            }

            if (position.HasTakeProfit)
            {
                IsTakeProfitEnabled = true;
                TakeProfitInPrice = position.TakeProfit;
                TakeProfitInPips = Symbol.Data.GetPipsFromPrice(Math.Abs(TakeProfitInPrice - Price));
            }
            else
            {
                IsTakeProfitEnabled = false;
                TakeProfitInPrice = default;
                TakeProfitInPips = default;
            }
        }

        public void OnSymbolTick()
        {
            if (TradeSide == ProtoOATradeSide.Buy)
            {
                Pips = Symbol.Data.GetPipsFromPrice(Symbol.Bid - Price);
            }
            else
            {
                Pips = Symbol.Data.GetPipsFromPrice(Price - Symbol.Ask);
            }

            GrossProfit = Math.Round(Pips * Symbol.PipValue * MonetaryConverter.FromMonetary(Volume), 2);
            NetProfit = Math.Round(GrossProfit + DoubleCommissionMonetary + SwapMonetary, 2);
        }

        public MarketOrderModel Clone() => MemberwiseClone() as MarketOrderModel;

        #region Equality

        public override bool Equals(object obj)
        {
            return Equals(obj as MarketOrderModel);
        }

        public bool Equals(MarketOrderModel other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(MarketOrderModel left, MarketOrderModel right)
        {
            return EqualityComparer<MarketOrderModel>.Default.Equals(left, right);
        }

        public static bool operator !=(MarketOrderModel left, MarketOrderModel right)
        {
            return !(left == right);
        }

        #endregion Equality
    }
}