using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    class GraphViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private readonly GraphBehaviour _graphBehaviour;
        private readonly Canvas _graphBackground;
        private const int MaxPointCount = 25000;

        public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _graphBackground = new Canvas();
            var b = new BehaviourManager {AllowMultipleEnabled = true};
            _graphBehaviour = new GraphBehaviour(_graphBackground) { IsEnabled = true };
            _graphBehaviour.ZoomRequested += (o, e) =>
                                   {
                                       var filteredPoints =
                                           _dataPoints.Select(
                                               dataPointSet =>
                                               dataPointSet.Where(
                                                   x =>
                                                   x.X >= (DateTime) e.FirstPoint.X && x.X >= (DateTime) e.SecondPoint.X))
                                               .ToList();
                                       SampleValues(MaxPointCount, filteredPoints);
                                   };
            _graphBehaviour.ZoomResetRequested += o => SampleValues(MaxPointCount, _dataPoints);
            b.Behaviours.Add(_graphBehaviour);
            Behaviour = b;
        }

        private List<DataSeries<DateTime, float>> _chartSeries = new List<DataSeries<DateTime, float>>();
        public List<DataSeries<DateTime, float>> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }

        private string _chartTitle = String.Empty;
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(()=> ChartTitle); } }

        private string _yAxisTitle = String.Empty;
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        private DoubleRange _range = new DoubleRange(0,0);
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        private IBehaviour _behaviour = new BehaviourManager();
        public IBehaviour Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        private double _minimum = 0;
        public double Minimum { get { return _minimum; } set { _minimum = value; NotifyOfPropertyChange(() => Minimum); NotifyOfPropertyChange(() => MinimumValue); MinimumMaximum = Minimum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1; } }

        public string MinimumValue { get { return string.Format("Minimum Value: {0}", (int)Minimum); } }

        private double _maximumMinimum;
        public double MaximumMinimum { get { return _maximumMinimum; } set { _maximumMinimum = value; NotifyOfPropertyChange(() => MaximumMinimum); } }

        private double _minimumMinimum;
        public double MinimumMinimum { get { return _minimumMinimum; } set { _minimumMinimum = value; NotifyOfPropertyChange(() => MinimumMinimum); } }

        private double _maximum = 0;
        public double Maximum { get { return _maximum; } set { _maximum = value; NotifyOfPropertyChange(() => Maximum); NotifyOfPropertyChange(() => MaximumValue); MaximumMinimum = Maximum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1; } }

        public string MaximumValue { get { return string.Format("Maximum Value: {0}", (int)Maximum); } }

        private double _maximumMaximum;
        public double MaximumMaximum { get { return _maximumMaximum; } set { _maximumMaximum = value; NotifyOfPropertyChange(() => MaximumMaximum); } }

        private double _minimumMaximum;
        public double MinimumMaximum { get { return _minimumMaximum; } set { _minimumMaximum = value; NotifyOfPropertyChange(() => MinimumMaximum); } }

        private bool _sampledValues;
        public bool SampledValues { get { return _sampledValues; } set { _sampledValues = value; NotifyOfPropertyChange(() => SampledValuesString); } }
        public string SampledValuesString { get { return (SampledValues) ? "WARNING SAMPLED VALUES" : String.Empty; } }

        private List<IEnumerable<DataPoint<DateTime, float>>> _dataPoints = new List<IEnumerable<DataPoint<DateTime, float>>>();
        private List<string> _seriesNames = new List<string>();
        public Sensor Sensor { set
        {
            _dataPoints.Add(from dataValue in value.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
            _seriesNames.Add(value.Name);
            ChartTitle = value.Name;
            YAxisTitle = value.Unit;
            SampleValues(MaxPointCount, _dataPoints);

            MaximumMaximum = MaximumY().Y + 100;
            MinimumMinimum = MinimumY().Y - 100;

            Maximum = MaximumMaximum;
        }}

        public List<Sensor> Sensors { set
        {
            ChartTitle = value[0].Name;
            for(int i = 0; i < value.Count(); i++)
            {
                _dataPoints.Add(from dataValue in value[i].CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
                _seriesNames.Add(value[i].Name);

                if (i > 0)
                    ChartTitle += " vs " + value[i].Name;
            }
            YAxisTitle = (((from dataSeries in value select dataSeries.Unit).Distinct()).Count() == 1) ? value[0].Unit : String.Empty;
            SampleValues(MaxPointCount, _dataPoints);

            MaximumMaximum = MaximumY().Y + 100;
            MinimumMinimum = MinimumY().Y - 100;

            Maximum = MaximumMaximum;
            Minimum = MinimumMinimum;
        }}

        private DataPoint<DateTime, float> MaximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in series)
                {
                    if (maxY == null)
                        maxY = value;
                    else if (value.Y > maxY.Y)
                        maxY = value;
                }
            }
            if(maxY == null)
                return new DataPoint<DateTime, float>(DateTime.Now,10);
            return maxY;
        }

        private DataPoint<DateTime, float> MinimumY()
        {
            DataPoint<DateTime, float> minY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in series)
                {
                    if (minY == null)
                        minY = value;
                    else if (value.Y < minY.Y)
                        minY = value;
                }
            }
            if (minY == null)
                return new DataPoint<DateTime, float>(DateTime.Now, 0);
            System.Diagnostics.Debug.Print("Lowest Y value {0}", minY.Y);
            return minY;
        }

        private void SampleValues(int numberOfPoints, IList<IEnumerable<DataPoint<DateTime, float>>> dataSource)
        {
            var generatedSeries = new List<DataSeries<DateTime, float>>();
            HideBackground();
            for (var i = 0; i < dataSource.Count; i++)
            {
                var sampleRate = dataSource[i].Count() / (numberOfPoints / dataSource.Count());
                System.Diagnostics.Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", dataSource[i].Count(), numberOfPoints, sampleRate);
                var series = (sampleRate > 1) ? new DataSeries<DateTime, float>(_seriesNames[i], dataSource[i].Where((x, index) => index % sampleRate == 0)) : new DataSeries<DateTime, float>(_seriesNames[i], dataSource[i]);
                generatedSeries.Add(series);
                if (sampleRate > 1) ShowBackground();
            }
            ChartSeries = generatedSeries;
            _graphBehaviour.RefreshVisual();
        }

        private void ShowBackground()
        {
            _graphBackground.Visibility = Visibility.Visible;
            SampledValues = true;
        }

        private void HideBackground()
        {
            _graphBackground.Visibility = Visibility.Collapsed;
            SampledValues = false;
        }
    }
}
