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
using System.Windows.Forms;
using CheckBox = System.Windows.Controls.CheckBox;

namespace IndiaTango.ViewModels
{
    public class GraphViewModel : BaseViewModel
    {
        #region Private fields

        private readonly Canvas _graphBackground;
        private readonly List<GraphableSensor> _selectedSensors = new List<GraphableSensor>();

        private List<GraphableSensor> _sensors;
        private BehaviourManager _behaviourManager;
        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private string _chartTitle = String.Empty;
        private string _yAxisTitle = String.Empty;
        private bool _columnVisible = true;
        private bool ColumnVisible { get { return _columnVisible; } set { _columnVisible = value; NotifyOfPropertyChange(() => ColumnWidth); NotifyOfPropertyChange(() => ToggleButtonImage); } }
        private bool _sampledValues;
        private bool SampledValues { get { return _sampledValues; } set { _sampledValues = value; NotifyOfPropertyChange(() => SampledValuesString); } }
        private int _sampleRate;
        private GraphableSensor _selectedSensor;
        private DateTime _startDateTime;
        private DateTime _endDateTime;
        private bool _selectionMode;
        private List<String> _samplingCaps = new List<string>();
        private int _samplingCapIndex;

        #region YAxisControls

        private DoubleRange _range = new DoubleRange(0, 0);
        private double _minimum;
        private double _minimumMinimum;
        private double _maximumMinimum;
        private double _maximum;
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

            _graphBackground = new Canvas { Visibility = Visibility.Collapsed };

            var behaviourManager = new BehaviourManager { AllowMultipleEnabled = true };

            var zoomBehaviour = new CustomZoomBehaviour { IsEnabled = !_selectionMode };
            zoomBehaviour.ZoomRequested += (o, e) =>
                                                 {
                                                     StartTime = (DateTime)e.FirstPoint.X;
                                                     EndTime = (DateTime)e.SecondPoint.X;
                                                     foreach (var sensor in _selectedSensors)
                                                     {
                                                         sensor.SetUpperAndLowerBounds(StartTime, EndTime);
                                                     }
                                                     SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
                                                 };
            zoomBehaviour.ZoomResetRequested += o =>
                                                      {
                                                          foreach (var sensor in _selectedSensors)
                                                          {
                                                              sensor.RemoveBounds();
                                                          }
                                                          SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
                                                          CalculateDateTimeEndPoints();
                                                      };

            behaviourManager.Behaviours.Add(zoomBehaviour);

            var selectionBehaviour = new CustomSelectionBehaviour { IsEnabled = _selectionMode };
            selectionBehaviour.SelectionMade += (o, e) =>
                                                    {
                                                        Debug.WriteLine("GraphView has recieved the selection over!");
                                                        Debug.WriteLine("If you read this the code doesn't do anything");
                                                    };
            selectionBehaviour.SelectionReset += o =>
                                                     {
                                                         Debug.WriteLine("GraphViewModel has recieved the selection reset!");
                                                         Debug.WriteLine("If you read this the code doesn't do anything");
                                                     };
            behaviourManager.Behaviours.Add(selectionBehaviour);

            var backgroundBehaviour = new GraphBackgroundBehaviour(_graphBackground) { IsEnabled = true };

            behaviourManager.Behaviours.Add(backgroundBehaviour);

            Behaviour = behaviourManager;

            SamplingCaps = new List<string>(Common.GenerateSamplingCaps());
            SelectedSamplingCapIndex = 3;
        }

        #region Public Parameters

        /// <summary>
        /// The list of sensors to display on the view
        /// </summary>
        public List<Sensor> SensorList { set { _sensors = (from x in value select new GraphableSensor(x)).ToList(); NotifyOfPropertyChange(() => GraphableSensors); } }

        public Dataset Dataset { get; set; }

        /// <summary>
        /// The list of GraphableSensors that can be used for graphing
        /// </summary>
        public List<GraphableSensor> GraphableSensors { get { return _sensors; } }

        /// <summary>
        /// The behaviour of the graph
        /// </summary>
        public BehaviourManager Behaviour { get { return _behaviourManager; } set { _behaviourManager = value; NotifyOfPropertyChange(() => Behaviour); } }

        /// <summary>
        /// The list of Line Series that the Chart pulls from
        /// </summary>
        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }

        /// <summary>
        /// The title of the Chart
        /// </summary>
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(() => ChartTitle); NotifyOfPropertyChange(() => Title); } }

        public string Title
        {
            get { return string.Format("[{0}] {1}", Dataset != null ? Dataset.IdentifiableName : Common.UnknownSite, ChartTitle == "" ? "Graph Data" : ChartTitle); }
        }

        /// <summary>
        /// The Y Axis Title
        /// </summary>
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        /// <summary>
        /// The Y Axis range
        /// </summary>
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        #region YAxisControls

        /// <summary>
        /// The value of the lower Y Axis range
        /// </summary>
        public double Minimum { get { return _minimum; } set { _minimum = value; NotifyOfPropertyChange(() => Minimum); NotifyOfPropertyChange(() => MinimumValue); MinimumMaximum = Minimum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1; } }

        /// <summary>
        /// The minimum value as a readable string
        /// </summary>
        public string MinimumValue
        {
            get { return string.Format("{0:N2}", Minimum); }
            set
            {
                var old = Minimum;
                try
                {
                    Minimum = double.Parse(value);
                }
                catch (Exception)
                {
                    Minimum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the bottom range can reach
        /// </summary>
        public double MaximumMinimum { get { return _maximumMinimum; } set { _maximumMinimum = value; NotifyOfPropertyChange(() => MaximumMinimum); } }

        /// <summary>
        /// The lowest value the bottom range can reach
        /// </summary>
        public double MinimumMinimum { get { return _minimumMinimum; } set { _minimumMinimum = value; NotifyOfPropertyChange(() => MinimumMinimum); } }

        /// <summary>
        /// The value of the high Y Axis range
        /// </summary>
        public double Maximum { get { return _maximum; } set { _maximum = value; NotifyOfPropertyChange(() => Maximum); NotifyOfPropertyChange(() => MaximumValue); MaximumMinimum = Maximum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1; } }

        /// <summary>
        /// The maximum value as a readable string
        /// </summary>
        public string MaximumValue
        {
            get { return string.Format("{0:N2}", Maximum); }
            set
            {
                var old = Maximum;
                try
                {
                    Maximum = double.Parse(value);
                }
                catch (Exception)
                {
                    Maximum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the top range can reach
        /// </summary>
        public double MaximumMaximum { get { return _maximumMaximum; } set { _maximumMaximum = value; NotifyOfPropertyChange(() => MaximumMaximum); } }

        /// <summary>
        /// The lowest value the top range can reach
        /// </summary>
        public double MinimumMaximum { get { return _minimumMaximum; } set { _minimumMaximum = value; NotifyOfPropertyChange(() => MinimumMaximum); } }

        #endregion

        /// <summary>
        /// The width of the column
        /// </summary>
        public int ColumnWidth { get { return _columnVisible ? 430 : 0; } }

        /// <summary>
        /// The column toggle buttons image
        /// </summary>
        public ImageSource ToggleButtonImage { get { return _columnVisible ? new BitmapImage(new Uri("pack://application:,,,/Images/expand_left.png")) : new BitmapImage(new Uri("pack://application:,,,/Images/expand_right.png")); } }

        /// <summary>
        /// A string form for the current sampling rate
        /// </summary>
        public string SampledValuesString { get { return (SampledValues) ? "Sampling every " + _sampleRate + " values" : String.Empty; } }

        /// <summary>
        /// The currently selected sensor
        /// </summary>
        public GraphableSensor SelectedSensor { get { return _selectedSensor; } set { _selectedSensor = value; NotifyOfPropertyChange(() => SelectedSensorColour); NotifyOfPropertyChange(() => SelectedSensorName); NotifyOfPropertyChange(() => SelectedSensor); NotifyOfPropertyChange(() => SelectedSensorVisibility); } }

        /// <summary>
        /// The selected sensors colour
        /// </summary>
        public Color SelectedSensorColour { get { return (_selectedSensor == null) ? Colors.Black : _selectedSensor.Colour; } set { if (_selectedSensors != null) _selectedSensor.Colour = value; NotifyOfPropertyChange(() => SelectedSensorColour); if (_selectedSensors.Contains(_selectedSensor)) RedrawGraph(); } }

        /// <summary>
        /// The name of the selected sensor
        /// </summary>
        public string SelectedSensorName { get { return (_selectedSensor == null) ? string.Empty : _selectedSensor.Sensor.Name; } }

        /// <summary>
        /// Decides if we should hide the sensor visibility or not
        /// </summary>
        public Visibility SelectedSensorVisibility { get { return (_selectedSensor == null) ? Visibility.Collapsed : Visibility.Visible; } }

        /// <summary>
        /// The first DateTime of the graph
        /// </summary>
        public DateTime StartTime { get { return _startDateTime; } set { _startDateTime = value; NotifyOfPropertyChange(() => StartTime); NotifyOfPropertyChange(() => CanEditDates); } }

        /// <summary>
        /// The last DateTime of the graph
        /// </summary>
        public DateTime EndTime { get { return _endDateTime; } set { _endDateTime = value; NotifyOfPropertyChange(() => EndTime); NotifyOfPropertyChange(() => CanEditDates); } }

        /// <summary>
        /// Whether or not you can currently edit the dates or not
        /// </summary>
        public bool CanEditDates { get { return (_selectedSensors.Count() > 0); } }

        public List<String> SamplingCaps { get { return _samplingCaps; } set { _samplingCaps = value; NotifyOfPropertyChange(() => SamplingCaps); } }

        public Visibility ExportButtonVisible { get { return _selectedSensors.Count > 0 ? Visibility.Visible : Visibility.Collapsed; } }
        public int SelectedSamplingCapIndex
        {
            get { return _samplingCapIndex; }
            set
            {
                _samplingCapIndex = value;
                NotifyOfPropertyChange(() => SelectedSamplingCapIndex);
            }
        }

        #endregion

        #region Private Methods

        #region GraphBackground Modifiers

        /// <summary>
        /// Shows the background on the graph
        /// </summary>
        private void ShowBackground()
        {
            _graphBackground.Visibility = Visibility.Visible;
            SampledValues = true;
        }

        /// <summary>
        /// Hides the background on the graph
        /// </summary>
        private void HideBackground()
        {
            _graphBackground.Visibility = Visibility.Collapsed;
            SampledValues = false;
        }

        #endregion

        /// <summary>
        /// Adds a sensor to the selected sensors
        /// </summary>
        /// <param name="sensor">The sensor to add</param>
        private void AddSensor(GraphableSensor sensor)
        {
            Debug.WriteLine("Adding sensor {0} to selected sensors", sensor.Sensor);
            _selectedSensors.Add(sensor);
            RedrawGraph();
            NotifyOfPropertyChange(() => ExportButtonVisible);
        }

        /// <summary>
        /// Removes a sensor from the selected sensors
        /// </summary>
        /// <param name="sensor">The sensor to remove</param>
        private void RemoveSensor(GraphableSensor sensor)
        {
            if (!_selectedSensors.Contains(sensor)) return;

            Debug.WriteLine("Removing sensor {0} to selected sensors", sensor.Sensor);
            _selectedSensors.Remove(sensor);
            RedrawGraph();
            NotifyOfPropertyChange(() => ExportButtonVisible);
        }

        /// <summary>
        /// Redraws the graph
        /// </summary>
        private void RedrawGraph()
        {
            ChartTitle = (_selectedSensors.Count > 0) ? (string.IsNullOrWhiteSpace(_selectedSensors[0].Sensor.Depth.ToString())
                                   ? string.Format(" and {0}", _selectedSensors[0].Sensor.Name)
                                   : string.Format(" and {0} [{1}]", _selectedSensors[0].Sensor.Name,
                                                   _selectedSensors[0].Sensor.Depth)) : String.Empty;

            for (var i = 1; i < _selectedSensors.Count; i++)
                ChartTitle += (string.IsNullOrWhiteSpace(_selectedSensors[i].Sensor.Depth.ToString())
                                   ? string.Format(" and {0}", _selectedSensors[i].Sensor.Name)
                                   : string.Format(" and {0} [{1}]", _selectedSensors[i].Sensor.Name,
                                                   _selectedSensors[i].Sensor.Depth));

            YAxisTitle = ((from sensor in _selectedSensors select sensor.Sensor.Unit).Distinct().Count() == 1) ? _selectedSensors[0].Sensor.Unit : String.Empty;

            SampleValues(Common.MaximumGraphablePoints, _selectedSensors);

            MaximumMaximum = MaximumY().Y + 10;
            MinimumMinimum = MinimumY().Y - 10;

            Maximum = MaximumMaximum;
            Minimum = MinimumMinimum;

            CalculateDateTimeEndPoints();
        }

        /// <summary>
        /// Takes a list of sensors and graphs as much of it as it can, using sampling
        /// </summary>
        /// <param name="numberOfPoints">The maximum amount of points to graph (soft limit)</param>
        /// <param name="sensors">The collection of sensor to graph</param>
        private void SampleValues(int numberOfPoints, ICollection<GraphableSensor> sensors)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count() / (numberOfPoints / sensors.Count);
                Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
                if (_sampleRate > 1) ShowBackground();
            }

            ChartSeries = generatedSeries;
        }

        /// <summary>
        /// Calculates the maximum Y value in the graph
        /// </summary>
        /// <returns>The point containing the maximum Y value</returns>
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

        /// <summary>
        /// Calculates the minimum Y value in the graph
        /// </summary>
        /// <returns>The point containing the minimum Y value</returns>
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

        /// <summary>
        /// Finds the first and last date values from the graph
        /// </summary>
        private void CalculateDateTimeEndPoints()
        {
            var nullTime = DateTime.Now;
            var maximum = nullTime;
            var minimum = nullTime;
            foreach (var sensor in _selectedSensors)
            {
                if (sensor.DataPoints.Count() <= 0) continue;

                var first = sensor.DataPoints.First().X;
                var last = sensor.DataPoints.Last().X;

                if (first < minimum || minimum == nullTime)
                    minimum = first;

                if (last > maximum || maximum == nullTime)
                    maximum = last;
            }
            Debug.WriteLine("Calculated the first point {0} and the last point {1}", minimum, maximum);
            StartTime = minimum;
            EndTime = maximum;
            Debug.WriteLine("As a result start {0} and end {1}", StartTime, EndTime);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Toggles the visibility of the column
        /// </summary>
        public void ToggleColumn()
        {
            ColumnVisible = !ColumnVisible;
        }

        /// <summary>
        /// Fired if a sensor is checked
        /// </summary>
        /// <param name="e">The event arguments</param>
        public void SensorChecked(RoutedEventArgs e)
        {
            var checkbox = (CheckBox)e.Source;
            AddSensor((GraphableSensor)checkbox.Content);

            SelectedSensor = (GraphableSensor)checkbox.Content;
        }

        /// <summary>
        /// Fired if a sensor is unchecked
        /// </summary>
        /// <param name="e">The event arguments</param>
        public void SensorUnchecked(RoutedEventArgs e)
        {
            var checkbox = (CheckBox)e.Source;
            RemoveSensor((GraphableSensor)checkbox.Content);
        }

        /// <summary>
        /// Exports the graph to a png
        /// </summary>
        /// <param name="chart"></param>
        public void ExportGraph(Chart chart)
        {
            if (_selectedSensors.Count == 0)
            {
                Common.ShowMessageBox("No Graph Showing",
                                      "You haven't selected a sensor to graph so there is nothing to export!", false,
                                      false);
                return;
            }

            var exportView = (_container.GetInstance(typeof(ExportToImageViewModel), "ExportToImageViewModel") as ExportToImageViewModel);
            if (exportView == null)
            {
                EventLogger.LogError(null, "Image Exporter", "Failed to get a export image view");
                return;
            }

            //Set up the view
            exportView.Chart = chart;
            exportView.SelectedSensors = _selectedSensors.ToArray();

            //Show the dialog
            _windowManager.ShowDialog(exportView);
        }

        /// <summary>
        /// Fired when the start date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void StartTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null)
                return;

            if (e.OldValue == null || (DateTime)e.OldValue == new DateTime() || (DateTime)e.NewValue < EndTime)
                StartTime = (DateTime)e.NewValue;
            else
                StartTime = (DateTime)e.OldValue;

            foreach (var sensor in _selectedSensors)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }
            SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
        }

        /// <summary>
        /// Fired when the end date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null)
                return;

            if (e.OldValue == null || (DateTime)e.OldValue == new DateTime() || (DateTime)e.NewValue > StartTime)
                EndTime = (DateTime)e.NewValue;
            else
                EndTime = (DateTime)e.OldValue;

            foreach (var sensor in _selectedSensors)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }
            SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
        }

        public void SamplingCapChanged(SelectionChangedEventArgs e)
        {
            try
            {
                Common.MaximumGraphablePoints = int.Parse((string)e.AddedItems[0]);
            }
            catch (Exception)
            {
                Common.MaximumGraphablePoints = int.MaxValue;
            }

            SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
        }

        #endregion
    }
}
