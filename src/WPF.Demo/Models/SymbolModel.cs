using OpenAPI.Net.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace Trading.UI.Demo.Models
{
    public class SymbolModel : BindableBase
    {
        public double Bid { get; private set; }

        public double Ask { get; private set; }

        public ProtoOALightSymbol LightSymbol { get; init; }

        public ProtoOASymbol Data { get; init; }

        public ProtoOAAsset BaseAsset { get; init; }

        public ProtoOAAsset QuoteAsset { get; init; }

        public List<SymbolModel> ConversionSymbols { get; } = new List<SymbolModel>();

        public string Name => LightSymbol.SymbolName;

        public long Id => LightSymbol.SymbolId;

        public double TickSize => Data.GetTickSize();

        public double PipSize => Data.GetPipSize();

        public double PipValue => Data.GetPipValue(TickValue);

        public double TickValue { get; set; }

        public event Action<SymbolModel> Tick;

        public void OnTick(double bid, double ask)
        {
            if (bid != Bid)
            {
                Bid = bid;

                RaisePropertyChanged(nameof(Bid));
            }

            if (ask != Ask)
            {
                Ask = ask;

                RaisePropertyChanged(nameof(Ask));
            }

            Tick?.Invoke(this);
        }
    }
}