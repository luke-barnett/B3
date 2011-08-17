using System;
using Caliburn.Micro;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;
using System.Windows;

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
            _selectedPoints.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_selectedPoints_CollectionChanged);
        }

        void _selectedPoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems.Count < 1)
                return;
            foreach(var item in e.NewItems)
            {
                var cast = (item as DataPoint<DateTime, float>);
                MessageBox.Show(string.Format("You selected the value {0} with the timestamp of {1}", cast.Y, cast.X));
            }
        }

        private DataSeries<DateTime, float> _chartSeries = new DataSeries<DateTime, float>();
        public DataSeries<DateTime, float> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }

        public string ChartTitle { get; set; }
        public string YAxisTitle { get; set; }

        private UniqueAndNotNullShadowedObservableCollection<object> _selectedPoints = new UniqueAndNotNullShadowedObservableCollection<object>();
        public UniqueAndNotNullShadowedObservableCollection<object> SelectedPoints { get { return _selectedPoints; } }

    }
}
