using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using IndiaTango.Models;
using System.Linq;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    class GraphViewModel : BaseViewModel
    {
        #region Private fields

        private const int MaxPointCount = 15000;

        private readonly GraphBehaviour _graphBehaviour;
        private readonly Canvas _graphBackground;

        private List<GraphableSensor> _sensors;
        private List<GraphableSensor> _selectedSensors = new List<GraphableSensor>();
        private BehaviourManager _behaviourManager;
        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private string _chartTitle = String.Empty;
        private string _yAxisTitle = String.Empty;
        private bool _columnVisible = true;
        private bool ColumnVisible { get { return _columnVisible; } set { _columnVisible = value; NotifyOfPropertyChange(() => ColumnWidth); NotifyOfPropertyChange(() => ToggleButtonImage); } }
        private bool _sampledValues;
        private bool SampledValues { get { return _sampledValues; } set { _sampledValues = value; NotifyOfPropertyChange(() => SampledValuesString); } }
        private int _sampleRate;

        #region YAxisControls

        private DoubleRange _range = new DoubleRange(0,0);
        private double _minimum = 0;
        private double _minimumMinimum;
        private double _maximumMinimum;
        private double _maximum = 0;
        private double _minimumMaximum;
        private double _maximumMaximum;

        #endregion

        #region Caliburn fields

        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;

        #endregion

        #endregion


        public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            _graphBackground = new Canvas {Visibility = Visibility.Collapsed};

            var behaviourManager = new BehaviourManager {AllowMultipleEnabled = true};

            _graphBehaviour = new GraphBehaviour(_graphBackground) {IsEnabled = true};
            _graphBehaviour.ZoomRequested += (o, e) => SampleValues(MaxPointCount , (from sensor in _selectedSensors select new GraphableSensor(sensor, (DateTime)e.FirstPoint.X, (DateTime)e.SecondPoint.X)).ToList());
            _graphBehaviour.ZoomResetRequested += o => SampleValues(MaxPointCount, _selectedSensors);
            
            behaviourManager.Behaviours.Add(_graphBehaviour);
            Behaviour = behaviourManager;
        }

        #region Public Parameters

        public List<Sensor> SensorList { set { _sensors = (from x in value select new GraphableSensor(x)).ToList(); NotifyOfPropertyChange(() => GraphableSensors); } }

        public List<GraphableSensor> GraphableSensors { get { return _sensors; } }

        public BehaviourManager Behaviour { get { return _behaviourManager; } set { _behaviourManager = value; NotifyOfPropertyChange(() => Behaviour); } }

        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }

        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(() => ChartTitle); } }

        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        #region YAxisControls

        public double Minimum { get { return _minimum; } set { _minimum = value; NotifyOfPropertyChange(() => Minimum); NotifyOfPropertyChange(() => MinimumValue); MinimumMaximum = Minimum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1; } }

        public string MinimumValue { get { return string.Format("Y Axis Min: {0}", (int)Minimum); } }

        public double MaximumMinimum { get { return _maximumMinimum; } set { _maximumMinimum = value; NotifyOfPropertyChange(() => MaximumMinimum); } }

        public double MinimumMinimum { get { return _minimumMinimum; } set { _minimumMinimum = value; NotifyOfPropertyChange(() => MinimumMinimum); } }

        public double Maximum { get { return _maximum; } set { _maximum = value; NotifyOfPropertyChange(() => Maximum); NotifyOfPropertyChange(() => MaximumValue); MaximumMinimum = Maximum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1; } }

        public string MaximumValue { get { return string.Format("Y Axis Max: {0}", (int)Maximum); } }

        public double MaximumMaximum { get { return _maximumMaximum; } set { _maximumMaximum = value; NotifyOfPropertyChange(() => MaximumMaximum); } }

        public double MinimumMaximum { get { return _minimumMaximum; } set { _minimumMaximum = value; NotifyOfPropertyChange(() => MinimumMaximum); } }

        #endregion

        public int ColumnWidth { get { return _columnVisible ? 250 : 0; } }

        public ImageSource ToggleButtonImage { get { return _columnVisible ? new BitmapImage(new Uri("pack://application:,,,/Images/expand_left.png")) : new BitmapImage(new Uri("pack://application:,,,/Images/expand_right.png")); } }

        public string SampledValuesString { get { return (SampledValues) ? "Sampling every " + _sampleRate + " values" : String.Empty; } }

        #endregion

        #region Private Methods

        #region GraphBackground Modifiers

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

        #endregion

        private void AddSensor(GraphableSensor sensor)
        {
            Debug.WriteLine("Adding sensor {0} to selected sensors", sensor.Sensor);
            _selectedSensors.Add(sensor);
            RedrawGraph();
        }

        private void RemoveSensor(GraphableSensor sensor)
        {
            if (!_selectedSensors.Contains(sensor)) return;

            Debug.WriteLine("Removing sensor {0} to selected sensors", sensor.Sensor);
            _selectedSensors.Remove(sensor);
            RedrawGraph();
        }

        private void RedrawGraph()
        {
            ChartTitle = (_selectedSensors.Count > 0) ? _selectedSensors[0].Sensor.Name : String.Empty;

            for (var i = 1; i < _selectedSensors.Count; i++)
                ChartTitle += " vs. " + _selectedSensors[i].Sensor.Name;

            YAxisTitle = ((from sensor in _selectedSensors select sensor.Sensor.Unit).Distinct().Count() == 1) ? _selectedSensors[0].Sensor.Unit : String.Empty;

            SampleValues(MaxPointCount, _selectedSensors);

            MaximumMaximum = MaximumY().Y + 100;
            MinimumMinimum = MinimumY().Y - 100;

            Maximum = MaximumMaximum;
            Minimum = MinimumMinimum;
        }

        private void SampleValues(int numberOfPoints, List<GraphableSensor> sensors)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count()/(numberOfPoints/sensors.Count);
                Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                generatedSeries.Add(new LineSeries{ DataSeries = series, LineStroke = sensor.Colour});
                if (_sampleRate > 1) ShowBackground();
            }

            ChartSeries = generatedSeries;
            _graphBehaviour.RefreshVisual();
        }

        private DataPoint<DateTime, float> MaximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in (DataSeries<DateTime, float>)series.DataSeries)
                {
                    if (maxY == null)
                        maxY = value;
                    else if (value.Y > maxY.Y)
                        maxY = value;
                }
            }
            if (maxY == null)
                return new DataPoint<DateTime, float>(DateTime.Now, 10);
            return maxY;
        }

        private DataPoint<DateTime, float> MinimumY()
        {
            DataPoint<DateTime, float> minY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in (DataSeries<DateTime, float>)series.DataSeries)
                {
                    if (minY == null)
                        minY = value;
                    else if (value.Y < minY.Y)
                        minY = value;
                }
            }
            if (minY == null)
                return new DataPoint<DateTime, float>(DateTime.Now, 0);
            return minY;
        }

        #endregion

        #region Event Handlers

        public void btnColumnToggle()
        {
            ColumnVisible = !ColumnVisible;
        }

        public void SensorChecked(RoutedEventArgs e)
        {
            var checkbox = (CheckBox) e.Source;
            AddSensor((GraphableSensor)checkbox.Content);
        }

        public void SensorUnchecked(RoutedEventArgs e)
        {
            var checkbox = (CheckBox)e.Source;
            RemoveSensor((GraphableSensor)checkbox.Content);
        }

        public void btnExportGraph()
        {
            Common.ShowFeatureNotImplementedMessageBox();
        }

        #endregion
    }
}
