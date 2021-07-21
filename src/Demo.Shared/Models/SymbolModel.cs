using OpenAPI.Net.Helpers;
using System;
using System.Collections.Generic;

namespace Samples.Shared.Models
{
    public class SymbolModel
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

        public event Action<SymbolQuote> Tick;

        public void OnTick(SymbolQuote quote)
        {
            if (quote.Bid != Bid)
            {
                Bid = quote.Bid;
            }

            if (quote.Ask != Ask)
            {
                Ask = quote.Ask;
            }

            Tick?.Invoke(quote);
        }
    }
}