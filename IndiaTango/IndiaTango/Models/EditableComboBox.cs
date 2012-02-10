using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace IndiaTango.Models
{
    /// <summary>
    /// Extension of a ComboBox for Helper classes
    /// </summary>
    public class EditableComboBox : ComboBox
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.Enter || Text.Length <= 0) return;

            AddToHelper();
            SelectedItem = Text;
        }

        public static readonly DependencyProperty HelperProperty =
            DependencyProperty.Register("Helper", typeof(string), typeof(EditableComboBox), new PropertyMetadata(default(string)));

        public string Helper
        {
            get { return (string)GetValue(HelperProperty); }
            set { SetValue(HelperProperty, value); }
        }

        private void AddToHelper()
        {
            if (Helper == null)
                return;

            if (String.CompareOrdinal(Helper, "Units") == 0)
            {
                UnitsHelper.Add(Text);
            }
            else if (String.CompareOrdinal(Helper, "Manufacturers") == 0)
            {
                ManufacturerHelper.Add(Text);
            }
            else if (String.CompareOrdinal(Helper, "Descriptions") == 0)
            {
                DescriptionHelper.Add(Text);
            }
            else if (String.CompareOrdinal(Helper, "SensorVocabulary") == 0)
            {
                SensorVocabulary.Add(Text);
            }
        }
    }
}
