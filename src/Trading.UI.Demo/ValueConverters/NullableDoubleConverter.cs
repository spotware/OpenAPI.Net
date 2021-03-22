using System;
using System.Globalization;
using System.Windows.Data;

namespace Trading.UI.Demo.ValueConverters
{
    [ValueConversion(typeof(double?), typeof(string))]
    public class NullableDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? doubleValue = (double?)value;

            return !doubleValue.HasValue || doubleValue.Value == default ? string.Empty : doubleValue.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value.ToString();

            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            else
            {
                return double.Parse(stringValue);
            }
        }
    }
}