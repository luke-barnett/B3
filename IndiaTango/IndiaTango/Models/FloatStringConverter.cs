using System;
using System.Globalization;
using System.Windows.Data;

namespace IndiaTango.Models
{
    /// <summary>
    /// Converts between float and string
    /// </summary>
    [ValueConversion(typeof(float), typeof(string))]
    public class FloatStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.ToString() : "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value != null ? float.Parse((string)value) : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
