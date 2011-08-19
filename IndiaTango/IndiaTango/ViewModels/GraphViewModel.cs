using System;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;

namespace IndiaTango.ViewModels
{
    class GraphViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;

        public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }
        
        private DataSeries<DateTime, float> _chartSeries = new DataSeries<DateTime, float>();
        public DataSeries<DateTime, float> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }

        public string ChartTitle { get; set; }
        public string YAxisTitle { get; set; }

        //TODO BE ABLE TO CHANGE THE MAJOR TICK INTERVAL
        private int _majorTickInterval = 50;
        public int MajorTickInterval { get { return _majorTickInterval; } set { _majorTickInterval = value; NotifyOfPropertyChange(() => MajorTickInterval);} }
    }
}
