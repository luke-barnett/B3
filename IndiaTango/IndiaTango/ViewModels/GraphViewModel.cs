using System;
using Caliburn.Micro;
using Visiblox.Charts;

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
            InitialiseGraph();
        }

        public void InitialiseGraph()
        {
            LineSeries l = new LineSeries();
            DataSeries<DateTime, float> data = new DataSeries<DateTime, float>();
            Random generator = new Random();

            for (int i = 0; i < 5000; i++)
            {
                data.Add((DateTime.Now).AddDays(i), (float)generator.NextDouble());
            }
            l.DataSeries = data;

            ChartSeries = data;
        }

        private DataSeries<DateTime, float> _chartSeries = new DataSeries<DateTime, float>();
        public DataSeries<DateTime, float> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }
    }
}
