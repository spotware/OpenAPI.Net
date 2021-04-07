using System;

namespace OpenAPI.Net.Helpers
{
    public static class MonetaryConverter
    {
        public static double FromMonetary(long monetary) => monetary / 100.0;

        public static long ToMonetary(double amount) => Convert.ToInt64(amount * 100);
    }
}