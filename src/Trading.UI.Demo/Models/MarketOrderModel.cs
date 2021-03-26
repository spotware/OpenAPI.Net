using System;
using System.Collections.Generic;
using Trading.UI.Demo.Helpers;

namespace Trading.UI.Demo.Models
{
    public class MarketOrderModel : OrderModel, IEquatable<MarketOrderModel>
    {
        private double _marketRangeInPips = 10;

        private bool _isMarketRange;

        private long _mirroringCommission, _swap, _usedMargin, _commission;

        private double _marginRate;

        private bool _guaranteedStopLoss;

        private ProtoOAPositionStatus _status;

        private ProtoOAOrderTriggerMethod _stopTriggerMethod;
        private double _pips;
        private double _netProfit;
        private double _grossProfit;
        private double _commissionMonetary;
        private double _swapMonetary;
        private double _usedMarginMonetary;

        public MarketOrderModel()
        {
        }

        public MarketOrderModel(ProtoOAPosition position, SymbolModel symbol) => Update(position, symbol);

        public double MarketRangeInPips { get => _marketRangeInPips; set => SetProperty(ref _marketRangeInPips, value); }

        public bool IsMarketRange { get => _isMarketRange; set => SetProperty(ref _isMarketRange, value); }

        public bool GuaranteedStopLoss
        {
            get => _guaranteedStopLoss;
            set => SetProperty(ref _guaranteedStopLoss, value);
        }

        public double MarginRate
        {
            get => _marginRate;
            set => SetProperty(ref _marginRate, value);
        }

        public long MirroringCommission
        {
            get => _mirroringCommission;
            set => SetProperty(ref _mirroringCommission, value);
        }

        public long Swap
        {
            get => _swap;
            set
            {
                SetProperty(ref _swap, value);

                SwapMonetary = Swap / Math.Pow(10, MoneyDigits);
            }
        }

        public double SwapMonetary { get => _swapMonetary; set => SetProperty(ref _swapMonetary, value); }

        public long UsedMargin
        {
            get => _usedMargin;
            set
            {
                SetProperty(ref _usedMargin, value);

                UsedMarginMonetary = UsedMargin / Math.Pow(10, MoneyDigits);
            }
        }

        public double UsedMarginMonetary { get => _usedMarginMonetary; set => SetProperty(ref _usedMarginMonetary, value); }

        public int MoneyDigits { get; set; }

        public long Commission
        {
            get => _commission;
            set
            {
                SetProperty(ref _commission, value);

                CommissionMonetary = Commission / Math.Pow(10, MoneyDigits);

                DoubleCommissionMonetary = CommissionMonetary * 2;
            }
        }

        public double CommissionMonetary { get => _commissionMonetary; private set => SetProperty(ref _commissionMonetary, value); }
        public double DoubleCommissionMonetary { get; private set; }

        public ProtoOAPositionStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ProtoOAOrderTriggerMethod StopTriggerMethod
        {
            get => _stopTriggerMethod;
            set => SetProperty(ref _stopTriggerMethod, value);
        }

        public double Pips { get => _pips; set => SetProperty(ref _pips, value); }

        public double NetProfit { get => _netProfit; set => SetProperty(ref _netProfit, value); }

        public double GrossProfit { get => _grossProfit; set => SetProperty(ref _grossProfit, value); }

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
                StopLossInPips = Symbol.GetPipsFromPrice(Math.Abs(StopLossInPrice - Price));
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
                TakeProfitInPips = Symbol.GetPipsFromPrice(Math.Abs(TakeProfitInPrice - Price));
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
                Pips = Symbol.GetPipsFromPrice(Symbol.Bid - Price);
            }
            else
            {
                Pips = Symbol.GetPipsFromPrice(Price - Symbol.Ask);
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