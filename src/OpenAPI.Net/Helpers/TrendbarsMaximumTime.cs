using ProtoOA.Enums;
using System;

namespace OpenAPI.Net.Helpers
{
    public static class TrendbarsMaximumTime
    {
        public static TimeSpan GetMaximumTime(this TrendbarPeriod period)
        {
            switch (period)
            {
                case TrendbarPeriod.M1:
                case TrendbarPeriod.M2:
                case TrendbarPeriod.M3:
                case TrendbarPeriod.M4:
                case TrendbarPeriod.M5:
                    return TimeSpan.FromMilliseconds(302400000);

                case TrendbarPeriod.M10:
                case TrendbarPeriod.M15:
                case TrendbarPeriod.M30:
                case TrendbarPeriod.H1:
                    return TimeSpan.FromMilliseconds(21168000000);

                case TrendbarPeriod.H4:
                case TrendbarPeriod.H12:
                case TrendbarPeriod.D1:
                    return TimeSpan.FromMilliseconds(31622400000);

                case TrendbarPeriod.W1:
                case TrendbarPeriod.Mn1:
                    return TimeSpan.FromMilliseconds(158112000000);

                default:
                    throw new ArgumentOutOfRangeException(nameof(period));
            }
        }
    }
}