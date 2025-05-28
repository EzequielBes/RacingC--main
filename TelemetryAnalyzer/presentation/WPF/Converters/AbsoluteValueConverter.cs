using System;
using System.Globalization;
using System.Windows.Data;

namespace TelemetryAnalyzer.Presentation.WPF.Converters // Or Controls, or a dedicated Converters namespace
{
    [ValueConversion(typeof(float), typeof(double))]
    public class AbsoluteValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float floatValue)
            {
                return (double)Math.Abs(floatValue);
            }
            if (value is double doubleValue)
            {
                return Math.Abs(doubleValue);
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not typically needed for one-way display
            throw new NotImplementedException();
        }
    }
}

