using System;
using System.Globalization;
using System.Windows.Data;

namespace Trading.UI.Demo.ValueConverters
{
    [ValueConversion(typeof(double?), typeof(string))]
    public class NullableToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.ToString() : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}