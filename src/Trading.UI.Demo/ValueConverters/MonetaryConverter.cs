using System;
using System.Globalization;
using System.Windows.Data;

namespace Trading.UI.Demo.ValueConverters
{
    [ValueConversion(typeof(long), typeof(long))]
    public class MonetaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return 0;
            }

            var monetary = System.Convert.ToInt64(value);

            return monetary / 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : System.Convert.ToInt64(value) * 100;
        }
    }
}