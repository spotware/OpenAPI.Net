using Prism.Mvvm;

namespace Trading.UI.Demo.Models
{
    public abstract class OrderModel : BindableBase
    {
        private SymbolModel _symbol;
        private ProtoOATradeSide _tradeSide;
        private double _volumeInLots;
        private bool _isStopLossEnabled;
        private double _stopLossInPips;
        private bool _isTrailingStopLossEnabled;
        private bool _isTakeProfitEnabled;
        private double _takeProfitInPips;
        private string _comment;
        private string _label;

        public SymbolModel Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }

        public ProtoOATradeSide TradeSide { get => _tradeSide; set => SetProperty(ref _tradeSide, value); }

        public double VolumeInLots { get => _volumeInLots; set => SetProperty(ref _volumeInLots, value); }

        public bool IsStopLossEnabled { get => _isStopLossEnabled; set => SetProperty(ref _isStopLossEnabled, value); }

        public double StopLossInPips { get => _stopLossInPips; set => SetProperty(ref _stopLossInPips, value); }

        public bool IsTrailingStopLossEnabled { get => _isTrailingStopLossEnabled; set => SetProperty(ref _isTrailingStopLossEnabled, value); }

        public bool IsTakeProfitEnabled { get => _isTakeProfitEnabled; set => SetProperty(ref _isTakeProfitEnabled, value); }

        public double TakeProfitInPips { get => _takeProfitInPips; set => SetProperty(ref _takeProfitInPips, value); }

        public string Comment { get => _comment; set => SetProperty(ref _comment, value); }

        public string Label { get => _label; set => SetProperty(ref _label, value); }

        public long Id { get; set; }
    }
}