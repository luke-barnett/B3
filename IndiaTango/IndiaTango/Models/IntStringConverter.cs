using System;
using System.Globalization;
using System.Windows.Data;

namespace IndiaTango.Models
{
    /// <summary>
    /// Converts integers to strings
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    public class IntStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.ToString() : "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value != null ? int.Parse((string)value) : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
