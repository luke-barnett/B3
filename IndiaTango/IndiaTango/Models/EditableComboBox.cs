using System.Windows.Controls;
using System.Windows.Input;

namespace IndiaTango.Models
{
    public class EditableComboBox : ComboBox
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.Enter || Text.Length <= 0) return;

            UnitsHelper.Add(Text);
            SelectedItem = Text;
        }
    }
}
