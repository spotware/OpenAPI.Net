using System;
using System.Globalization;
using System.Windows.Data;
using Trading.UI.Demo.Helpers;

namespace Trading.UI.Demo.ValueConverters
{
    [ValueConversion(typeof(long), typeof(double))]
    public class MonetaryValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return 0;
            }

            var monetary = System.Convert.ToInt64(value);

            return MonetaryConverter.FromMonetary(monetary);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : MonetaryConverter.ToMonetary(System.Convert.ToInt64(value));
        }
    }
}