using OpenAPI.Net.Helpers;
using System;

namespace ASP.NET.Demo.Models
{
    public abstract class OrderModel
    {
        private SymbolModel _symbol;
        private bool _isStopLossEnabled;
        private bool _isTakeProfitEnabled;
        private double _price;
        private double _stopLossInPrice;
        private double _takeProfitInPrice;

        public SymbolModel Symbol
        {
            get => _symbol;
            set
            {
                _symbol = value;

                Volume = _symbol.Data.NormalizeVolume(Volume);
            }
        }

        public ProtoOATradeSide TradeSide { get; set; }

        public long Volume { get; set; }

        public bool IsStopLossEnabled
        {
            get => _isStopLossEnabled;
            set
            {
                if (_isStopLossEnabled == value) return;

                _isStopLossEnabled = value;

                if (value is true && StopLossInPips == default)
                {
                    StopLossInPips = 10;
                }
                else if (value is false)
                {
                    StopLossInPips = default;
                }
            }
        }

        public double StopLossInPips { get; set; }

        public bool IsTrailingStopLossEnabled { get; set; }

        public bool IsTakeProfitEnabled
        {
            get => _isTakeProfitEnabled;
            set
            {
                if (_isTakeProfitEnabled == value) return;

                _isTakeProfitEnabled = value;

                if (value is true && TakeProfitInPips == default)
                {
                    TakeProfitInPips = 10;
                }
                else if (value is false)
                {
                    TakeProfitInPips = default;
                }
            }
        }

        public double TakeProfitInPips { get; set; }

        public string Comment { get; set; }

        public string Label { get; set; }

        public long Id { get; set; }

        public long RelativeStopLoss => Symbol.Data.GetRelativeFromPips(StopLossInPips);

        public long RelativeTakeProfit => Symbol.Data.GetRelativeFromPips(TakeProfitInPips);

        public double StopLossInPrice
        {
            get => _stopLossInPrice;
            set => _stopLossInPrice = Symbol is not null ? Math.Round(value, Symbol.Data.Digits) : value;
        }

        public double TakeProfitInPrice
        {
            get => _takeProfitInPrice;
            set => _takeProfitInPrice = Symbol is not null ? Math.Round(value, Symbol.Data.Digits) : value;
        }

        public double Price
        {
            get => _price;
            set => _price = Symbol is not null ? Math.Round(value, Symbol.Data.Digits) : value;
        }

        public ProtoOATradeData TradeData { get; set; }

        public DateTimeOffset OpenTime { get; set; }

        public DateTimeOffset LastUpdateTime { get; set; }
    }
}