using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPI.Net.Helpers
{
    public static class SymbolExtensions
    {
        public static double GetPipValue(this ProtoOASymbol symbol, double tickValue) => tickValue * (symbol.GetPipSize() / symbol.GetTickSize());

        public static double GetTickSize(this ProtoOASymbol symbol) => 1 / Math.Pow(10, symbol.Digits);

        public static double GetTickValue(this ProtoOASymbol symbol, ProtoOAAsset symbolQuoteAsset, ProtoOAAsset accountDepositAsset, IEnumerable<Tuple<ProtoOAAsset, ProtoOAAsset, double>> conversionAssets)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));
            _ = symbolQuoteAsset ?? throw new ArgumentNullException(nameof(symbolQuoteAsset));
            _ = accountDepositAsset ?? throw new ArgumentNullException(nameof(accountDepositAsset));

            double tickValue;

            var symbolTickSize = symbol.GetTickSize();

            if (symbolQuoteAsset.AssetId == accountDepositAsset.AssetId)
            {
                tickValue = symbolTickSize;
            }
            else
            {
                tickValue = symbolTickSize * Convert(symbolQuoteAsset, conversionAssets);
            }

            return tickValue;
        }

        public static double Convert(this ProtoOAAsset symbolQuoteAsset, IEnumerable<Tuple<ProtoOAAsset, ProtoOAAsset, double>> conversionAssets)
        {
            _ = conversionAssets ?? throw new ArgumentNullException(nameof(conversionAssets));

            if (!conversionAssets.Any()) throw new ArgumentOutOfRangeException(nameof(conversionAssets), conversionAssets, $"The {nameof(conversionAssets)} is empty");
            if (conversionAssets.Any(asset => asset.Item3 is 0)) throw new ArgumentOutOfRangeException(nameof(conversionAssets), conversionAssets, $"Some of the '{nameof(conversionAssets)}' price values or zero");

            double result = 1;

            var asset = symbolQuoteAsset;

            foreach (var (baseAsset, quoteAsset, price) in conversionAssets)
            {
                if (asset.AssetId == baseAsset.AssetId)
                {
                    result *= price;

                    asset = quoteAsset;
                }
                else
                {
                    result /= price;

                    asset = baseAsset;
                }
            }

            return result;
        }

        public static double GetPipSize(this ProtoOASymbol symbol) => 1 / Math.Pow(10, symbol.PipPosition);

        public static long GetRelativeFromPips(this ProtoOASymbol symbol, double pips)
        {
            var pipsInPrice = pips * symbol.GetPipSize();

            return (long)Math.Round(pipsInPrice * 100000, symbol.Digits);
        }

        public static double GetPriceFromRelative(this ProtoOASymbol symbol, long relative) => Math.Round(relative / 100000.0, symbol.Digits);

        public static double GetPipsFromRelative(this ProtoOASymbol symbol, long relative) => Math.Round((relative / 100000.0) / symbol.GetPipSize(), symbol.Digits - symbol.PipPosition);

        public static double GetPipsFromPoints(this ProtoOASymbol symbol, long points) => symbol.GetPipsFromPrice(points * symbol.GetTickSize());

        public static long GetPointsFromPips(this ProtoOASymbol symbol, double pips) => System.Convert.ToInt64(pips * Math.Pow(10, symbol.Digits - symbol.PipPosition));

        public static double GetPipsFromPrice(this ProtoOASymbol symbol, double price) => Math.Round(price * Math.Pow(10, symbol.PipPosition), symbol.Digits - symbol.PipPosition);

        public static long NormalizeVolume(this ProtoOASymbol symbol, long volumeInUnits)
        {
            var normalizedVolume = volumeInUnits - (volumeInUnits % symbol.StepVolume);

            if (normalizedVolume > symbol.MaxVolume) normalizedVolume = symbol.MaxVolume;
            if (normalizedVolume < symbol.MinVolume) normalizedVolume = symbol.MinVolume;

            return normalizedVolume;
        }

        public static double AddPipsToPrice(this ProtoOASymbol symbol, double price, double pips)
        {
            var pipsInPrice = pips * symbol.GetPipSize();

            return Math.Round(price + pipsInPrice, symbol.Digits);
        }

        public static double SubtractPipsFromPrice(this ProtoOASymbol symbol, double price, double pips)
        {
            var pipsInPrice = pips * symbol.GetPipSize();

            return Math.Round(price - pipsInPrice, symbol.Digits);
        }
    }
}