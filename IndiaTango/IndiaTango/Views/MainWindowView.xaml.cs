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
    }
}
