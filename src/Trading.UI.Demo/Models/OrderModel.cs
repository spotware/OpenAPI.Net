using Prism.Mvvm;

namespace Trading.UI.Demo.Models
{
    public abstract class OrderModel : BindableBase
    {
        private SymbolModel _symbol;
        private ProtoOATradeSide _tradeSide = ProtoOATradeSide.Buy;
        private long _volume;
        private bool _isStopLossEnabled;
        private double _stopLossInPips;
        private bool _isTrailingStopLossEnabled;
        private bool _isTakeProfitEnabled;
        private double _takeProfitInPips;
        private string _comment;
        private string _label;
        private double _price;

        public SymbolModel Symbol
        {
            get => _symbol;
            set
            {
                SetProperty(ref _symbol, value);

                Volume = _symbol.NormalizeVolume(Volume);
            }
        }

        public ProtoOATradeSide TradeSide { get => _tradeSide; set => SetProperty(ref _tradeSide, value); }

        public long Volume { get => _volume; set => SetProperty(ref _volume, value); }

        public bool IsStopLossEnabled
        {
            get => _isStopLossEnabled;
            set
            {
                if (SetProperty(ref _isStopLossEnabled, value) is false) return;

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

        public double StopLossInPips { get => _stopLossInPips; set => SetProperty(ref _stopLossInPips, value); }

        public bool IsTrailingStopLossEnabled { get => _isTrailingStopLossEnabled; set => SetProperty(ref _isTrailingStopLossEnabled, value); }

        public bool IsTakeProfitEnabled
        {
            get => _isTakeProfitEnabled;
            set
            {
                if (SetProperty(ref _isTakeProfitEnabled, value) is false) return;

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

        public double TakeProfitInPips { get => _takeProfitInPips; set => SetProperty(ref _takeProfitInPips, value); }

        public string Comment { get => _comment; set => SetProperty(ref _comment, value); }

        public string Label { get => _label; set => SetProperty(ref _label, value); }

        public long Id { get; set; }

        public long RelativeStopLoss => Symbol.GetRelativeFromPips(StopLossInPips);

        public long RelativeTakeProfit => Symbol.GetRelativeFromPips(TakeProfitInPips);

        public double StopLossInPrice
        {
            get
            {
                if (!IsStopLossEnabled) return default;

                return TradeSide == ProtoOATradeSide.Sell ? Symbol.AddPipsToPrice(Price, StopLossInPips) : Symbol.SubtractPipsFromPrice(Price, StopLossInPips);
            }
        }

        public double TakeProfitInPrice
        {
            get
            {
                if (!IsTakeProfitEnabled) return default;

                return TradeSide == ProtoOATradeSide.Sell ? Symbol.SubtractPipsFromPrice(Price, TakeProfitInPips) : Symbol.AddPipsToPrice(Price, TakeProfitInPips);
            }
        }

        public double Price { get => _price; set => SetProperty(ref _price, value); }
    }
}