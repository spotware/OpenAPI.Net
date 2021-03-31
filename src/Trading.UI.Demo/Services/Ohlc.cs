using System;

namespace Trading.UI.Demo.Services
{
    public class Ohlc
    {
        public double Open { get; init; }

        public double High { get; init; }

        public double Low { get; init; }

        public double Close { get; init; }

        public DateTimeOffset Time { get; init; }
    }
}