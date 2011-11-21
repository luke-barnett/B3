using System.Windows;

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
