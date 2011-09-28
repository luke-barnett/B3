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
    /// Interaction logic for SpecifyValueView.xaml
    /// </summary>
    public partial class SpecifyValueView : Window
    {
        public SpecifyValueView()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//Stupid damn tab order.
			if (txtValue.Visibility == Visibility.Visible)
				txtValue.Focus();
			else
				comboValue.Focus();
		}
    }
}
