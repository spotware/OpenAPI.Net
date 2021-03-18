using System;

namespace Trading.UI.Demo.Models
{
    public class SymbolModel
    {
        public ProtoOALightSymbol LightSymbol { init; get; }

        public ProtoOASymbol Data { init; get; }

        public string Name => LightSymbol.SymbolName;

        public long Id => LightSymbol.SymbolId;

        public double TickSize => 1 / Math.Pow(10, Data.Digits);

        public double PipSize => 1 / Math.Pow(10, Data.PipPosition);

        public long GetRelativeFromPips(double pips)
        {
            var pipsInPrice = pips * PipSize;

            return (long)Math.Round(pipsInPrice * 100000, Data.Digits);
        }

        public long NormalizeVolume(long volumeInUnits)
        {
            var normalizedVolume = volumeInUnits - (volumeInUnits % Data.StepVolume);

            if (normalizedVolume > Data.MaxVolume) normalizedVolume = Data.MaxVolume;
            if (normalizedVolume < Data.MinVolume) normalizedVolume = Data.MinVolume;

            return normalizedVolume;
        }
    }
}