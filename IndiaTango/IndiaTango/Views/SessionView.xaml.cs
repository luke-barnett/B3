using System.Windows;
using System.Windows.Controls;
using IndiaTango.Models;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for SessionView.xaml
    /// </summary>
    public partial class SessionView : Window
    {
        public SessionView()
        {
            InitializeComponent();
        }

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Common.SetFancyBackground(this, grdMain,false, true);
		}

        private void UpdateSensorName(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) return;
            txtName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorDescription(object sender, TextChangedEventArgs e)
        {
            txtDescription.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorLowerLimit(object sender, TextChangedEventArgs e)
        {
            txtLowerLimit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorUpperLimit(object sender, TextChangedEventArgs e)
        {
            txtUpperLimit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorUnit(object sender, TextChangedEventArgs e)
        {
            txtUnit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorMaxRateOfChange(object sender, TextChangedEventArgs e)
        {
            txtMaxRateOfChange.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorManufacturer(object sender, TextChangedEventArgs e)
        {
            txtManufacturer.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorSerialNumber(object sender, TextChangedEventArgs e)
        {
            txtSerial.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorErrorThreshold(object sender, TextChangedEventArgs e)
        {
            txtErrorThreshold.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void UpdateSensorSummaryMode(object sender, SelectionChangedEventArgs e)
        {
            comboSummary.GetBindingExpression(ComboBox.SelectedIndexProperty).UpdateSource();
        }

        private void UpdateSensorDepth(object sender, TextChangedEventArgs e)
        {
            txtDepth.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
    }
}
