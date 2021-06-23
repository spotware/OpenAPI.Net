using OpenAPI.Net.Helpers;
using System;
using System.Collections.Generic;

namespace ASP.NET.Demo.Models
{
    public class PendingOrderModel : OrderModel, IEquatable<PendingOrderModel>
    {
        public PendingOrderModel()
        {
        }

        public PendingOrderModel(ProtoOAOrder order, SymbolModel symbol) => Update(order, symbol);

        public bool IsFilledOrCanceled { get; set; }

        public PendingOrderType Type { get; set; }

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

        public double LimitRangeInPips { get; set; }

        public bool IsExpiryEnabled { get; set; }

        public DateTimeOffset ExpiryTime { get; set; }

        public ProtoOAOrderStatus Status { get; set; }

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
                ExpiryTime = DateTimeOffset.FromUnixTimeMilliseconds(order.ExpirationTimestamp);

                IsExpiryEnabled = true;
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