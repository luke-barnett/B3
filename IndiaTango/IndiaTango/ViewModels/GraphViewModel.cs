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
        }

        public void InitialiseGraph()
        {
            LineSeries l = new LineSeries();
            DataSeries<DateTime, float> data = new DataSeries<DateTime, float>();
            Random generator = new Random();

            for (int i = 0; i < 50; i++)
            {
                data.Add(DateTime.Now, (float)generator.NextDouble());
            }
            l.DataSeries = data;
            _chartSeries.Add(l);
            NotifyOfPropertyChange("ChartSeries");
        }

        private SeriesCollection<IChartSeries> _chartSeries;
        public SeriesCollection<IChartSeries> ChartSeries { get
        {
         if(_chartSeries == null)
             _chartSeries = new SeriesCollection<IChartSeries>();
            return _chartSeries;
        } set { _chartSeries = value; } }
    }
}
