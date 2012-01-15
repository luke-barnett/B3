using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace IndiaTango.Models
{
    [ValueConversion(typeof(string), typeof(Brush))]
    public class CellForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DataRowView))
                return Brushes.Black;

            var rowView = (DataRowView) value;

            var hasEditedValues = rowView.Row.ItemArray.Where(item => item is string).Cast<string>().Any(x => x.IndexOf('[') != -1);
            
            return hasEditedValues ? Brushes.Red : Brushes.Black;;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
