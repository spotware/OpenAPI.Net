using System;

namespace Trading.UI.Demo.Models
{
    public class SymbolModel
    {
        public double Bid { get; set; }

        public double Ask { get; set; }
        public ProtoOALightSymbol LightSymbol { get; init; }

        public ProtoOASymbol Data { get; init; }

        public ProtoOAAsset BaseAsset { get; init; }

        public ProtoOAAsset QuoteAsset { get; init; }

        public string Name => LightSymbol.SymbolName;

        public long Id => LightSymbol.SymbolId;

        public double TickSize => 1 / Math.Pow(10, Data.Digits);

        public double PipSize => 1 / Math.Pow(10, Data.PipPosition);

        public double PipValue => TickValue * (PipSize / TickSize);

        public double TickValue { get; set; }

        public long GetRelativeFromPips(double pips)
        {
            var pipsInPrice = pips * PipSize;

            return (long)Math.Round(pipsInPrice * 100000, Data.Digits);
        }

        public double GetPriceFromRelative(long relative) => Math.Round(relative / 100000.0, Data.Digits);

        public double GetPipsFromRelative(long relative) => Math.Round((relative / 100000.0) / PipSize, Data.Digits - Data.PipPosition);

        public double GetPipsFromPoints(long points) => GetPipsFromPrice(points * TickSize);

        public long GetPointsFromPips(double pips) => Convert.ToInt64(pips * Math.Pow(10, Data.Digits - Data.PipPosition));

        public double GetPipsFromPrice(double price) => Math.Round(price * Math.Pow(10, Data.PipPosition), Data.Digits - Data.PipPosition);

        public long NormalizeVolume(long volumeInUnits)
        {
            var normalizedVolume = volumeInUnits - (volumeInUnits % Data.StepVolume);

            if (normalizedVolume > Data.MaxVolume) normalizedVolume = Data.MaxVolume;
            if (normalizedVolume < Data.MinVolume) normalizedVolume = Data.MinVolume;

            return normalizedVolume;
        }

        public double AddPipsToPrice(double price, double pips)
        {
            var pipsInPrice = pips * PipSize;

            return Math.Round(price + pipsInPrice, Data.Digits);
        }

        public double SubtractPipsFromPrice(double price, double pips)
        {
            var pipsInPrice = pips * PipSize;

            return Math.Round(price - pipsInPrice, Data.Digits);
        }
    }
}