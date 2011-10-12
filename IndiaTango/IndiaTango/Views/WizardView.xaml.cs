using System.Windows;
using System.Windows.Controls;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for WizardView.xaml
    /// </summary>
    public partial class WizardView : Window
    {
        public WizardView()
        {
            InitializeComponent();

            wizardCongrats.Text = "You've successfully completed the wizard.\nClick 'Finish' to close the wizard.";
        }

        private void BtnNextClick(object sender, RoutedEventArgs e)
        {
            if (wizardTabs.SelectedIndex != 1)
                return;

            txtDescription.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtErrorThreshold.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtManufacturer.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtLowerLimit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtUpperLimit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtMaxRateOfChange.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtSerial.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtUnit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            comboSummary.GetBindingExpression(ComboBox.SelectedIndexProperty).UpdateSource();
        }
    }
}
