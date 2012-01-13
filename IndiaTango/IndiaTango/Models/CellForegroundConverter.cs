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

            foreach (var item in rowView.Row.ItemArray.Where(item => item is string).Cast<string>())
            {
                if (item == "")
                    return Brushes.Black;

                if (item.IndexOf('[') == -1)
                    return Brushes.Red;

                if (item.StartsWith("["))
                    return Brushes.Red;

                var parts = item.Split(' ');
                if (parts[0] != parts[1].Replace("[", "").Replace("]", ""))
                    return Brushes.Red;
            }
            
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
