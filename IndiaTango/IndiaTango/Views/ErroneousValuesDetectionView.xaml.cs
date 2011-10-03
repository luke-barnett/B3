using System.Windows.Controls;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for ErroneousValuesDetectionView.xaml
    /// </summary>
    public partial class ErroneousValuesDetectionView
    {
        public ErroneousValuesDetectionView()
        {
            InitializeComponent();
        }

        private void MinimumValueTextChanged(object sender, TextChangedEventArgs e)
        {
            var bindingExpression = MinimumValue.GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
                bindingExpression.UpdateSource();
        }

        private void MaximumValueTextChanged(object sender, TextChangedEventArgs e)
        {
            var bindingExpression = MaximumValue.GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
                bindingExpression.UpdateSource();
        }
    }
}
