using System.Windows;
using Caliburn.Micro;

namespace IndiaTango.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;

        public MainViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public string Title { get { return ApplicationTitle; } }

        public void BtnNew()
        {
            System.Diagnostics.Debug.Print("Window manager null {0}", _windowManager == null);
            System.Diagnostics.Debug.Print("container null {0}", _container == null);
            _windowManager.ShowDialog(_container.GetInstance(typeof(MainViewModel), "MainViewModel"));
        }
        
        public void BtnLoad()
        {
            MessageBox.Show("Sorry, not yet implemented");
        }

        public void BtnGraphView()
        {
            _windowManager.ShowDialog(_container.GetInstance(typeof(GraphViewModel), "GraphViewModel"));
        }
    }
}
