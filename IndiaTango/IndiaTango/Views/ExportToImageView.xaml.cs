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
    /// Interaction logic for ExportToImageView.xaml
    /// </summary>
    public partial class ExportToImageView : Window
    {
        public ExportToImageView()
        {
            InitializeComponent();
        }

        private void WidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            WidthTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            HeightTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
    }
}
