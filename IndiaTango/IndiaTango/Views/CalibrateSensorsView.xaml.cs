using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for MissingValues.xaml
    /// </summary>
    public partial class CalibrateSensorsView : Window
    {
        public CalibrateSensorsView()
        {
            InitializeComponent();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            txtFormula.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
        
        private void MaximumValue_KeyUp(object sender, KeyEventArgs e)
        {
            MaximumValue.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void MinimumValue_KeyUp(object sender, KeyEventArgs e)
        {
            MinimumValue.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
    }
}
