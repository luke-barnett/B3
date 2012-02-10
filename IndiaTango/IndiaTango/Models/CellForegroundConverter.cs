using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace IndiaTango.Models
{
    /// <summary>
    /// Converts cell values to their foreground colour to show edited values
    /// </summary>
    public class CellForegroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var cell = values[0] as DataGridCell;
            var row = values[1] as DataRow;

            if (cell == null  || row == null)
                return Brushes.Black;

            var isAnEditedValue = cell.Column.DisplayIndex != 0 && row.ItemArray[cell.Column.DisplayIndex] is string && (row.ItemArray[cell.Column.DisplayIndex] as string).Contains('[');

            return isAnEditedValue ? Brushes.Red : Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
