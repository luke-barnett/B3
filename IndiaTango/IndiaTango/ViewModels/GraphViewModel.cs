using System;
using Caliburn.Micro;
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
            _selectedPoints.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SelectedPointsCollectionChanged);
        }

        void SelectedPointsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems == null || e.NewItems.Count < 1)
                return;

            foreach(var item in e.NewItems)
                SelectedDataPoint = (item as DataPoint<DateTime, float>);
        }

        private DataPoint<DateTime, float> _selectedPoint = null;
 
        public DataPoint<DateTime, float> SelectedDataPoint
        {
            get { return _selectedPoint; }
            set
            {
                _selectedPoint = value;
                NotifyOfPropertyChange(() => SelectedDataValue);
                NotifyOfPropertyChange(() => SelectedTimestamp);
            }
        }

        public string SelectedDataValue
        {
            get
            {
                if (SelectedDataPoint != null)
                    return SelectedDataPoint.Y.ToString();

                return "";
            }
        }

        public string SelectedTimestamp
        {
            get
            {
                if (SelectedDataPoint != null)
                    return SelectedDataPoint.X.ToString();

                return "";
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
