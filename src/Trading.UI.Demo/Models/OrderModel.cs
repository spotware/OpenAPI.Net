using Prism.Mvvm;

namespace Trading.UI.Demo.Models
{
    public abstract class OrderModel : BindableBase
    {
        private SymbolModel _symbol;
        private ProtoOATradeSide _tradeSide = ProtoOATradeSide.Buy;
        private long _volume;
        private bool _isStopLossEnabled;
        private double _stopLossInPips = 10;
        private bool _isTrailingStopLossEnabled;
        private bool _isTakeProfitEnabled;
        private double _takeProfitInPips = 10;
        private string _comment;
        private string _label;

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

        public bool IsStopLossEnabled { get => _isStopLossEnabled; set => SetProperty(ref _isStopLossEnabled, value); }

        public double StopLossInPips { get => _stopLossInPips; set => SetProperty(ref _stopLossInPips, value); }

        public bool IsTrailingStopLossEnabled { get => _isTrailingStopLossEnabled; set => SetProperty(ref _isTrailingStopLossEnabled, value); }

        public bool IsTakeProfitEnabled { get => _isTakeProfitEnabled; set => SetProperty(ref _isTakeProfitEnabled, value); }

        public double TakeProfitInPips { get => _takeProfitInPips; set => SetProperty(ref _takeProfitInPips, value); }

        public string Comment { get => _comment; set => SetProperty(ref _comment, value); }

        public string Label { get => _label; set => SetProperty(ref _label, value); }

        public long Id { get; set; }

        public long RelativeStopLoss => Symbol.GetRelativeFromPips(StopLossInPips);

        public long RelativeTakeProfit => Symbol.GetRelativeFromPips(TakeProfitInPips);
    }
}