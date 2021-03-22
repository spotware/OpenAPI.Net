using System;
using System.Collections.Generic;
using Trading.UI.Demo.Enums;

namespace Trading.UI.Demo.Models
{
    public class PendingOrderModel : OrderModel, IEquatable<PendingOrderModel>
    {
        private PendingOrderType _type;
        private double _limitRangeInPips;
        private bool _isExpiryEnabled;
        private DateTime _expiryTime = DateTime.Now;
        private ProtoOAOrderStatus _status;

        public PendingOrderModel()
        {
        }

        public PendingOrderModel(ProtoOAOrder order, SymbolModel symbol) => Update(order, symbol);

        public PendingOrderType Type { get => _type; set => SetProperty(ref _type, value); }

        public ProtoOAOrderType ProtoType
        {
            get => Type switch
            {
                PendingOrderType.Limit => ProtoOAOrderType.Limit,
                PendingOrderType.Stop => ProtoOAOrderType.Stop,
                PendingOrderType.StopLimit => ProtoOAOrderType.StopLimit,
                _ => throw new InvalidOperationException($"Order type {Type} is not valid")
            };
            set => Type = value switch
            {
                ProtoOAOrderType.Limit => PendingOrderType.Limit,
                ProtoOAOrderType.Stop => PendingOrderType.Stop,
                ProtoOAOrderType.StopLimit => PendingOrderType.StopLimit,
                _ => throw new InvalidOperationException($"Order type {value} is not valid")
            };
        }

        public double LimitRangeInPips { get => _limitRangeInPips; set => SetProperty(ref _limitRangeInPips, value); }

        public bool IsExpiryEnabled { get => _isExpiryEnabled; set => SetProperty(ref _isExpiryEnabled, value); }

        public DateTime ExpiryTime { get => _expiryTime; set => SetProperty(ref _expiryTime, value); }

        public ProtoOAOrderStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public PendingOrderModel Clone() => MemberwiseClone() as PendingOrderModel;

        public void Update(ProtoOAOrder order, SymbolModel symbol)
        {
            Symbol = symbol;
            LastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(order.UtcLastUpdateTimestamp);
            Status = order.OrderStatus;
            StopLossInPrice = order.StopLoss;
            TakeProfitInPrice = order.TakeProfit;
            TradeData = order.TradeData;
            ProtoType = order.OrderType;
            Volume = order.TradeData.Volume;
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(order.TradeData.OpenTimestamp);
            Id = order.OrderId;
            Comment = order.TradeData.Comment;
            Label = order.TradeData.Label;

            Price = Type switch
            {
                PendingOrderType.Limit => order.LimitPrice,
                PendingOrderType.Stop or PendingOrderType.StopLimit => order.StopPrice,
                _ => throw new InvalidOperationException($"Order type {Type} is not valid")
            };

            if (order.HasExpirationTimestamp)
            {
                ExpiryTime = DateTimeOffset.FromUnixTimeMilliseconds(order.ExpirationTimestamp).LocalDateTime;
            }
            else
            {
                IsExpiryEnabled = false;

                ExpiryTime = default;
            }

            if (order.HasRelativeStopLoss)
            {
                IsStopLossEnabled = true;
                StopLossInPips = Symbol.GetPipsFromRelative(order.RelativeStopLoss);
                StopLossInPrice = TradeSide == ProtoOATradeSide.Sell ? Symbol.AddPipsToPrice(Price, StopLossInPips) : Symbol.SubtractPipsFromPrice(Price, StopLossInPips);

                IsTrailingStopLossEnabled = order.TrailingStopLoss;
            }
            else
            {
                IsStopLossEnabled = false;
                StopLossInPips = default;
                StopLossInPrice = default;

                IsTrailingStopLossEnabled = false;
            }

            if (order.HasRelativeTakeProfit)
            {
                IsTakeProfitEnabled = true;
                TakeProfitInPips = Symbol.GetPipsFromRelative(order.RelativeTakeProfit);
                TakeProfitInPrice = TradeSide == ProtoOATradeSide.Sell ? Symbol.SubtractPipsFromPrice(Price, TakeProfitInPips) : Symbol.AddPipsToPrice(Price, TakeProfitInPips);
            }
            else
            {
                IsTakeProfitEnabled = false;
                TakeProfitInPips = default;
                TakeProfitInPrice = default;
            }

            if (Type == PendingOrderType.StopLimit)
            {
                LimitRangeInPips = symbol.GetPipsFromPoints(order.SlippageInPoints);
            }
        }

        #region Equality

        public override bool Equals(object obj)
        {
            return Equals(obj as PendingOrderModel);
        }

        public bool Equals(PendingOrderModel other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(PendingOrderModel left, PendingOrderModel right)
        {
            return EqualityComparer<PendingOrderModel>.Default.Equals(left, right);
        }

        public static bool operator !=(PendingOrderModel left, PendingOrderModel right)
        {
            return !(left == right);
        }

        #endregion Equality
    }
}