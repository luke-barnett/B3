using System.Windows;
using System.Windows.Input;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();
            NameScope.SetNameScope(dataGridContextMenu, NameScope.GetNameScope(this));
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.S)
            {
                chkBoxSelectionMode.IsChecked = !chkBoxSelectionMode.IsChecked;
            }
        }
    }
}
