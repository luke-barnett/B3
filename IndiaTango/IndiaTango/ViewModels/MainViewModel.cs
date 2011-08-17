using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Caliburn.Micro;
using Visiblox.Charts;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
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
            MessageBox.Show("Shame");
        }

        public void BtnGraphView()
        {
            var graphViewModel = _container.GetInstance(typeof (GraphViewModel), "GraphViewModel") as GraphViewModel;
            if (graphViewModel != null)
            {
                graphViewModel.ChartTitle = "New Graph";
                var chartSeries = new DataSeries<DateTime, float>();
                var generator = new Random();
                for (int i = 0; i < 500; i++ )
                {
                    chartSeries.Add((DateTime.Now).AddHours(i), (float)generator.NextDouble()*i);
                }
                chartSeries.Title = "Random Data";
                graphViewModel.ChartSeries = chartSeries;
                graphViewModel.YAxisTitle = "Points of Awesome";
                _windowManager.ShowDialog(graphViewModel);
            }
            else
                MessageBox.Show("Failed to get a GraphViewModel");
        }
    }
}
