using System;

namespace OpenAPI.Net.Helpers
{
    public static class TrendbarsMaximumTime
    {
        public static TimeSpan GetMaximumTime(this ProtoOATrendbarPeriod period)
        {
            switch (period)
            {
                case ProtoOATrendbarPeriod.M1:
                case ProtoOATrendbarPeriod.M2:
                case ProtoOATrendbarPeriod.M3:
                case ProtoOATrendbarPeriod.M4:
                case ProtoOATrendbarPeriod.M5:
                    return TimeSpan.FromMilliseconds(302400000);

                case ProtoOATrendbarPeriod.M10:
                case ProtoOATrendbarPeriod.M15:
                case ProtoOATrendbarPeriod.M30:
                case ProtoOATrendbarPeriod.H1:
                    return TimeSpan.FromMilliseconds(21168000000);

                case ProtoOATrendbarPeriod.H4:
                case ProtoOATrendbarPeriod.H12:
                case ProtoOATrendbarPeriod.D1:
                    return TimeSpan.FromMilliseconds(31622400000);

                case ProtoOATrendbarPeriod.W1:
                case ProtoOATrendbarPeriod.Mn1:
                    return TimeSpan.FromMilliseconds(158112000000);

                default:
                    throw new ArgumentOutOfRangeException(nameof(period));
            }
        }
    }
}