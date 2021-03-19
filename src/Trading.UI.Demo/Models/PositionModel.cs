using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace Trading.UI.Demo.Models
{
    public class PositionModel : BindableBase, IEquatable<PositionModel>
    {
        #region Fields

        private long _id, _mirroringCommission, _swap, _usedMargin, _commission, _volume;

        private double _price, _marginRate;

        private double? _stopLoss, _takeProfit;

        private DateTimeOffset _lastUpdateTime;

        private bool _guaranteedStopLoss;

        private ProtoOAPositionStatus _status;

        private ProtoOAOrderTriggerMethod _stopTriggerMethod;

        private ProtoOATradeData _tradeData;

        private SymbolModel _symbol;

        #endregion Fields

        public PositionModel(ProtoOAPosition position, SymbolModel symbol) => Update(position, symbol);

        #region Properties

        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public SymbolModel Symbol
        {
            get => _symbol;
            set => SetProperty(ref _symbol, value);
        }

        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public long Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        public ProtoOAPositionStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public double? StopLoss
        {
            get => _stopLoss;
            set => SetProperty(ref _stopLoss, value);
        }

        public double? TakeProfit
        {
            get => _takeProfit;
            set => SetProperty(ref _takeProfit, value);
        }

        public ProtoOAOrderTriggerMethod StopTriggerMethod
        {
            get => _stopTriggerMethod;
            set => SetProperty(ref _stopTriggerMethod, value);
        }

        public ProtoOATradeData TradeData
        {
            get => _tradeData;
            set => SetProperty(ref _tradeData, value);
        }

        public DateTimeOffset LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public DateTimeOffset OpenTime => DateTimeOffset.FromUnixTimeMilliseconds(TradeData.OpenTimestamp);

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
            set => SetProperty(ref _swap, value);
        }

        public long UsedMargin
        {
            get => _usedMargin;
            set => SetProperty(ref _usedMargin, value);
        }

        public long Commission
        {
            get => _commission;
            set => SetProperty(ref _commission, value);
        }

        public double? StopLossInPips
        {
            get
            {
                if (!StopLoss.HasValue) return null;

                return Symbol.GetPipsFromPrice(Math.Abs(Price - StopLoss.Value));
            }
        }

        public double? TakeProfitInPips
        {
            get
            {
                if (!TakeProfit.HasValue) return null;

                return Symbol.GetPipsFromPrice(Math.Abs(Price - TakeProfit.Value));
            }
        }

        #endregion Properties

        public void Update(ProtoOAPosition position, SymbolModel symbol)
        {
            GuaranteedStopLoss = position.GuaranteedStopLoss;
            Id = position.PositionId;
            LastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(position.UtcLastUpdateTimestamp);
            MarginRate = position.MarginRate;
            MirroringCommission = position.MirroringCommission;
            Price = position.Price;
            Status = position.PositionStatus;
            StopLoss = position.StopLoss > 0 ? (double?)position.StopLoss : null;
            TakeProfit = position.TakeProfit > 0 ? (double?)position.TakeProfit : null;
            Swap = position.Swap;
            UsedMargin = (long)position.UsedMargin;
            TradeData = position.TradeData;
            StopTriggerMethod = position.StopLossTriggerMethod;
            Symbol = symbol;
            Commission = position.Commission;
            Volume = position.TradeData.Volume;
        }

        #region Equality

        public override bool Equals(object obj)
        {
            return Equals(obj as PositionModel);
        }

        public bool Equals(PositionModel other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }

        public static bool operator ==(PositionModel left, PositionModel right)
        {
            return EqualityComparer<PositionModel>.Default.Equals(left, right);
        }

        public static bool operator !=(PositionModel left, PositionModel right)
        {
            return !(left == right);
        }

        #endregion Equality
    }
}