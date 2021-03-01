using Prism.Mvvm;

namespace Trading.UI.Demo.Models
{
    public abstract class OrderModel : BindableBase
    {
        public ProtoOASymbol Symbol { get; set; }

        public ProtoOATradeSide TradeSide { get; set; }

        public double VolumeInLots { get; set; }

        public bool IsStopLossEnabled { get; set; }

        public double StopLossInPips { get; set; }

        public bool IsTakeProfitEnabled { get; set; }

        public double TakeProfitInPips { get; set; }

        public string Comment { get; set; }

        public string Label { get; set; }
    }
}