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
    /// Interaction logic for OutlierDetectionView.xaml
    /// </summary>
    public partial class OutlierDetectionView : Window
    {
        public OutlierDetectionView()
        {
            InitializeComponent();
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
