using System;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;

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
            _windowManager.ShowDialog(_container.GetInstance(typeof(SessionViewModel), "SessionViewModel"));
        }
        
        public void BtnLoad()
        {
            MessageBox.Show("Sorry.");
        }

        public void BtnGraphView()
        {
            var graphViewModel = _container.GetInstance(typeof (GraphViewModel), "GraphViewModel") as GraphViewModel;
            if (graphViewModel != null)
            {
                var sensor = new Sensor("Dummy State", "Points of Awesome");
                sensor.AddState(new SensorState(DateTime.Now));
                var generator = new Random();
                for (int i = 0; i < 5000; i++ )
                {
                    sensor.CurrentState.Values.Add(new DataValue(DateTime.Now.AddHours(i),(float)generator.NextDouble()*i));
                }
                graphViewModel.Sensor = sensor;
                _windowManager.ShowDialog(graphViewModel);
            }
            else
                MessageBox.Show("Failed to get a GraphViewModel");
        }
    }
}
