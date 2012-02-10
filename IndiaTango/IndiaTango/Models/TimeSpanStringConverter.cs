using System;
using System.Globalization;
using System.Windows.Data;

namespace IndiaTango.Models
{
    /// <summary>
    /// Converts between a timespan object and a string object
    /// </summary>
    [ValueConversion(typeof(TimeSpan), typeof(string))]
    class TimeSpanStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? ((TimeSpan)value).Days.ToString() : "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value != null ? new TimeSpan(int.Parse((string)value), 0, 0, 0) : new TimeSpan(0, 0, 0);
            }
            catch
            {
                return new TimeSpan(0, 0, 0);
            }
        }
    }
}
