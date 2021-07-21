using OpenAPI.Net.Helpers;
using System;
using System.Collections.Generic;
using Trading.UI.Sample.Enums;

namespace Trading.UI.Sample.Models
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
            Volume = order.TradeData.Volume;
            TradeSide = order.TradeData.TradeSide;
            Id = order.OrderId;
            TradeData = order.TradeData;
            LastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(order.UtcLastUpdateTimestamp);
            Status = order.OrderStatus;
            StopLossInPrice = order.StopLoss;
            TakeProfitInPrice = order.TakeProfit;
            ProtoType = order.OrderType;
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(order.TradeData.OpenTimestamp);
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
                StopLossInPips = Symbol.Data.GetPipsFromRelative(order.RelativeStopLoss);
                StopLossInPrice = TradeSide == ProtoOATradeSide.Sell ? Symbol.Data.AddPipsToPrice(Price, StopLossInPips) : Symbol.Data.SubtractPipsFromPrice(Price, StopLossInPips);

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
                TakeProfitInPips = Symbol.Data.GetPipsFromRelative(order.RelativeTakeProfit);
                TakeProfitInPrice = TradeSide == ProtoOATradeSide.Sell ? Symbol.Data.SubtractPipsFromPrice(Price, TakeProfitInPips) : Symbol.Data.AddPipsToPrice(Price, TakeProfitInPips);
            }
            else
            {
                IsTakeProfitEnabled = false;
                TakeProfitInPips = default;
                TakeProfitInPrice = default;
            }

            if (Type == PendingOrderType.StopLimit)
            {
                LimitRangeInPips = Symbol.Data.GetPipsFromPoints(order.SlippageInPoints);
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