using System;
using System.Globalization;
using System.Windows.Data;

namespace TelemetryAnalyzer.Presentation.WPF.Converters
{
    [ValueConversion(typeof(int), typeof(string))]
    public class GearToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int gear)
            {
                return gear switch
                {
                    0 => "R", // Reverse
                    1 => "N", // Neutral
                    _ when gear > 1 => (gear - 1).ToString(), // 2 becomes 1st, 3 becomes 2nd, etc.
                    _ => "?" // Unknown or invalid
                };
            }
            return "N"; // Default to Neutral if value is not an int
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for display purposes
            throw new NotImplementedException();
        }
    }
}

