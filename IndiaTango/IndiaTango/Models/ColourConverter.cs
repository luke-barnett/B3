using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IndiaTango.Models
{
    /// <summary>
    /// Converts between Colour and Color
    /// </summary>
    [ValueConversion(typeof(Colour), typeof(Color))]
    class ColourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var colour = (Colour) value;
            return Color.FromArgb(colour.A, colour.R, colour.G, colour.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Colour((Color) value);
        }
    }
}
