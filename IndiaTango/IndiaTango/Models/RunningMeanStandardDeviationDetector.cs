using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Detects erroneous values based on a standard deviation on a running mean
    /// </summary>
    public class RunningMeanStandardDeviationDetector : IDetectionMethod
    {
        private int _smoothingPeriod = 60;
        private int _requestedSmoothingPeriod = 60;
        private float _numberOfStandardDeviations = 1;
        private float _requestedNumerOfStandardDeviations = 1;
        private Dictionary<DateTime, float> _upperLine = new Dictionary<DateTime, float>();
        private Dictionary<DateTime, float> _lowerLine = new Dictionary<DateTime, float>();
        private bool _showGraph;
        private Grid _settings;
        private bool _isEnabled;

        /// <summary>
        /// Fired if the graph needs to be updated
        /// </summary>
        public event UpdateGraph GraphUpdateNeeded;

        /// <summary>
        /// Fired if the list of detected values needs to be updated
        /// </summary>
        public event Updated RefreshDetectedValues;

        //The current sensor
        private Sensor _currentSensor;

        //The sensor chosen to graph with
        private Sensor _graphedSensor;

        private ComboBox _sensorsCombo;

        public override string ToString()
        {
            return string.Empty;
        }

        public string Name
        {
            get { return "Running Mean with Standard Deviation"; }
        }

        public string Abbreviation
        {
            get { return "RMwSD"; }
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            if (sensorToCheck != _currentSensor)
                GenerateUpperAndLowerLines(sensorToCheck);

            var items = (from value in sensorToCheck.CurrentState.Values
                         where (_upperLine.ContainsKey(value.Key) && value.Value > _upperLine[value.Key]) || (_lowerLine.ContainsKey(value.Key) && value.Value < _lowerLine[value.Key])
                         select new ErroneousValue(value.Key, this, sensorToCheck)).ToList();
            return items;
        }

        public bool HasSettings
        {
            get { return true; }
        }

        public Grid SettingsGrid
        {
            get
            {
                if (_settings == null)
                {
                    var wrapper = new Grid();
                    var stackPanel = new StackPanel();

                    var graphGrid = new Grid();
                    var graphCheckBox = new CheckBox
                                            {
                                                Content = new TextBlock { Text = "Show Graph" },
                                                HorizontalAlignment = HorizontalAlignment.Left,
                                                IsChecked = _showGraph
                                            };

                    graphCheckBox.Checked += (o, e) =>
                                                 {
                                                     _showGraph = true;
                                                     GraphUpdateNeeded();
                                                 };
                    graphCheckBox.Unchecked += (o, e) =>
                                                   {
                                                       _showGraph = false;
                                                       GraphUpdateNeeded();
                                                   };

                    graphGrid.Children.Add(graphCheckBox);

                    var updateGraphButton = new Button
                                                {
                                                    Content = new TextBlock { Text = "Update Graph" },
                                                    HorizontalAlignment = HorizontalAlignment.Right
                                                };

                    updateGraphButton.Click += (o, e) => GraphUpdateNeeded();

                    graphGrid.Children.Add(updateGraphButton);

                    stackPanel.Children.Add(graphGrid);

                    var graphOptions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

                    graphOptions.Children.Add(new TextBlock
                                                  {
                                                      Text = "What sensor to use when graphing lines",
                                                      Margin = new Thickness(0, 0, 10, 0)
                                                  });

                    _sensorsCombo = new ComboBox { Width = 100 };

                    _sensorsCombo.SelectionChanged += (o, e) =>
                                                          {
                                                              if (e.AddedItems.Count < 1)
                                                                  return;
                                                              _graphedSensor = e.AddedItems[0] as Sensor;
                                                              if (_showGraph)
                                                                  GraphUpdateNeeded();
                                                          };

                    graphOptions.Children.Add(_sensorsCombo);

                    stackPanel.Children.Add(graphOptions);

                    var smoothingPeriodGrid = new Grid();
                    smoothingPeriodGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    smoothingPeriodGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var smoothingPeriodTitle = new TextBlock
                                                   {
                                                       Text = "Smoothing Period",
                                                       HorizontalAlignment = HorizontalAlignment.Left
                                                   };

                    Grid.SetRow(smoothingPeriodTitle, 0);
                    smoothingPeriodGrid.Children.Add(smoothingPeriodTitle);

                    var smoothingPeriodHoursText = new TextBlock
                                                       {
                                                           Text = _requestedSmoothingPeriod / 60 + " Hour(s)",
                                                           HorizontalAlignment = HorizontalAlignment.Right
                                                       };

                    Grid.SetRow(smoothingPeriodHoursText, 0);
                    smoothingPeriodGrid.Children.Add(smoothingPeriodHoursText);

                    var smoothingPeriodSlider = new Slider { Value = _requestedSmoothingPeriod / 60d, Maximum = 336, Minimum = 1 };
                    smoothingPeriodSlider.ValueChanged += (o, e) =>
                                                              {
                                                                  _requestedSmoothingPeriod = (int)e.NewValue * 60;
                                                                  smoothingPeriodHoursText.Text =
                                                                      _requestedSmoothingPeriod / 60 + " Hour(s)";
                                                              };
                    smoothingPeriodSlider.PreviewMouseUp += (o, e) =>
                                                                {
                                                                    if (_requestedSmoothingPeriod != _smoothingPeriod)
                                                                    {
                                                                        _smoothingPeriod = _requestedSmoothingPeriod;
                                                                        GenerateUpperAndLowerLines(_currentSensor);
                                                                        Debug.WriteLine("Refresh of values needed");
                                                                        RefreshDetectedValues();
                                                                        if (_showGraph)
                                                                            GraphUpdateNeeded();
                                                                    }

                                                                };
                    Grid.SetRow(smoothingPeriodSlider, 1);
                    smoothingPeriodGrid.Children.Add(smoothingPeriodSlider);

                    stackPanel.Children.Add(smoothingPeriodGrid);

                    var standarDeviationsGrid = new Grid();
                    standarDeviationsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    standarDeviationsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var standarDeviationsTitle = new TextBlock
                                                     {
                                                         Text = "Standard Deviations",
                                                         HorizontalAlignment = HorizontalAlignment.Left
                                                     };

                    Grid.SetRow(standarDeviationsTitle, 0);
                    standarDeviationsGrid.Children.Add(standarDeviationsTitle);

                    var standardDeviationsText = new TextBlock
                                                     {
                                                         Text = _requestedNumerOfStandardDeviations.ToString(),
                                                         HorizontalAlignment = HorizontalAlignment.Right
                                                     };

                    Grid.SetRow(standardDeviationsText, 0);
                    standarDeviationsGrid.Children.Add(standardDeviationsText);

                    var standarDeviationsSlider = new Slider
                                                      {
                                                          Value = _requestedNumerOfStandardDeviations,
                                                          Maximum = 5,
                                                          Minimum = 0,
                                                          TickFrequency = 0.05,
                                                          IsSnapToTickEnabled = true,
                                                          TickPlacement = TickPlacement.BottomRight
                                                      };
                    standarDeviationsSlider.ValueChanged += (o, e) =>
                                                                {
                                                                    _requestedNumerOfStandardDeviations =
                                                                        (float)e.NewValue;
                                                                    standardDeviationsText.Text =
                                                                        _requestedNumerOfStandardDeviations.ToString();
                                                                };
                    standarDeviationsSlider.PreviewMouseUp += (o, e) =>
                                                                  {
                                                                      if (
                                                                          Math.Abs(_requestedNumerOfStandardDeviations -
                                                                                   _numberOfStandardDeviations) > 0.01)
                                                                      {
                                                                          _numberOfStandardDeviations =
                                                                              _requestedNumerOfStandardDeviations;
                                                                          GenerateUpperAndLowerLines(_currentSensor);
                                                                          Debug.WriteLine("Refresh of values needed");
                                                                          RefreshDetectedValues();
                                                                          if (_showGraph)
                                                                              GraphUpdateNeeded();
                                                                      }
                                                                  };
                    Grid.SetRow(standarDeviationsSlider, 1);
                    standarDeviationsGrid.Children.Add(standarDeviationsSlider);

                    stackPanel.Children.Add(standarDeviationsGrid);

                    wrapper.Children.Add(stackPanel);
                    _settings = wrapper;
                }
                return _settings;
            }
        }

        public bool HasGraphableSeries
        {
            get { return (_sensorsCombo != null) ? (_showGraph && _sensorsCombo.SelectedIndex > -1) : (_showGraph && _upperLine.Count != 0 && _lowerLine.Count != 0); }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            if (sensor != _currentSensor)
                GenerateUpperAndLowerLines(sensor);

            if (!HasGraphableSeries || !_upperLine.ContainsKey(timeStamp) || !_lowerLine.ContainsKey(timeStamp) ||
                !sensor.CurrentState.Values.ContainsKey(timeStamp))
                return false;
            return sensor.CurrentState.Values[timeStamp] < _upperLine[timeStamp] &&
                   sensor.CurrentState.Values[timeStamp] > _lowerLine[timeStamp];
        }

        public List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate)
        {
            if (!HasGraphableSeries)
                return new List<LineSeries>();

            var upperLine = new LineSeries
                                {
                                    DataSeries =
                                        new DataSeries<DateTime, float>("Upper Deviation",
                                        (from point in _upperLine
                                         where point.Key >= startDate && point.Key <= endDate
                                         select new DataPoint<DateTime, float>(point.Key, point.Value)).OrderBy(x => x.X)),
                                    LineStroke = Brushes.OrangeRed
                                };
            var lowerLine = new LineSeries
                                {
                                    DataSeries =
                                        new DataSeries<DateTime, float>("Lower Deviation",
                                        (from point in _lowerLine
                                         where point.Key >= startDate && point.Key <= endDate
                                         select new DataPoint<DateTime, float>(point.Key, point.Value)).OrderBy(x => x.X)),
                                    LineStroke = Brushes.OrangeRed
                                };
            return new List<LineSeries> { upperLine, lowerLine };
        }

        public List<LineSeries> GraphableSeries(DateTime startDate, DateTime endDate)
        {
            if (!HasGraphableSeries || _sensorsCombo.SelectedIndex == -1)
                return new List<LineSeries>();

            if (_currentSensor != _graphedSensor)
                GenerateUpperAndLowerLines(_graphedSensor);

            var upperLine = new LineSeries
            {
                DataSeries =
                    new DataSeries<DateTime, float>("Upper Deviation",
                    (from point in _upperLine
                     where point.Key >= startDate && point.Key <= endDate
                     select new DataPoint<DateTime, float>(point.Key, point.Value)).OrderBy(x => x.X)),
                LineStroke = Brushes.OrangeRed
            };
            var lowerLine = new LineSeries
            {
                DataSeries =
                    new DataSeries<DateTime, float>("Lower Deviation",
                    (from point in _lowerLine
                     where point.Key >= startDate && point.Key <= endDate
                     select new DataPoint<DateTime, float>(point.Key, point.Value)).OrderBy(x => x.X)),
                LineStroke = Brushes.OrangeRed
            };
            return new List<LineSeries> { upperLine, lowerLine };
        }

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod>(); }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                if (IsEnabled && _showGraph)
                    GraphUpdateNeeded();
            }
        }

        /// <summary>
        /// The number of minutes to smooth over
        /// </summary>
        public int SmoothingPeriod { get { return _smoothingPeriod; } set { _smoothingPeriod = value; } }

        /// <summary>
        /// The number of standard deviations to use
        /// </summary>
        public float NumberOfStandardDeviations { get { return _numberOfStandardDeviations; } set { _numberOfStandardDeviations = value; } }

        /// <summary>
        /// Turns on or off Graphing
        /// </summary>
        public bool ShowGraph { set { _showGraph = value; } }

        private void GenerateUpperAndLowerLines(Sensor sensor)
        {
            //Set Current Sensor
            _currentSensor = sensor;

            if (_currentSensor == null)
                return;

            Debug.WriteLine("Generating Lines");
            //Reset lines
            _upperLine = new Dictionary<DateTime, float>();
            _lowerLine = new Dictionary<DateTime, float>();

            var timeGap = _currentSensor.Owner.DataInterval;

            foreach (var value in _currentSensor.CurrentState.Values)
            {
                var meanValues = new List<float>();
                for (var i = value.Key.AddMinutes(-(timeGap * (_smoothingPeriod / 2 / timeGap))); i < value.Key.AddMinutes((timeGap * (_smoothingPeriod / 2 / timeGap))); i = i.AddMinutes(timeGap))
                {
                    if (_currentSensor.CurrentState.Values.ContainsKey(i))
                        meanValues.Add(_currentSensor.CurrentState.Values[i]);
                }

                meanValues.Add(value.Value);

                var average = meanValues.Sum() / meanValues.Count;
                var sumOfSquares = meanValues.Sum(x => Math.Pow(x - average, 2));
                var standardDeviation = Math.Sqrt(sumOfSquares / (meanValues.Count - 1));

                if (double.IsNaN(standardDeviation))
                    standardDeviation = 0;

                _upperLine[value.Key] = (float)(average + (_numberOfStandardDeviations * standardDeviation));
                _lowerLine[value.Key] = (float)(average - (_numberOfStandardDeviations * standardDeviation));

                //Debug.Print("numberOfValues: {0} average: {1} sumOfSquare: {2} stdDev: {3} upper: {4} lower: {5}", meanValues.Count, average, sumOfSquares, standardDeviation, _upperLine[value.Key], _lowerLine[value.Key]);
            }
        }

        public ListBox ListBox { get; set; }

        public Sensor[] SensorOptions
        {
            set
            {
                _sensorsCombo.Items.Clear();
                foreach (var sensor in value)
                {
                    _sensorsCombo.Items.Add(sensor);
                }

                if (_sensorsCombo.Items.Count == 1)
                    _sensorsCombo.SelectedIndex = 0;
                else if (_graphedSensor != null && !_sensorsCombo.Items.Contains(_graphedSensor) && _showGraph)
                    GraphUpdateNeeded();
            }
        }

        public string About
        {
            get { return "This method is used to detect data that are extreme, relative to the rest of the time series for a given parameter.\r\n\nYou may specify a sensor to edit, a period over which to average, and the number of standard devations for which to set upper and lower limits. Select ‘Show on graph’ to display the standard deviation boundaries on the graph to the right. If you tick ‘Check detection methods for values’, timestamps for which data lie outside these bounds will be displayed in the ‘Detected values’ list . Values will be detected only within the date range displayed on the graph to the right. You can select any combination of the timestamps from the detected values list, and modify them using the ‘Delete’ (shortcut ‘Delete’ key), ‘Interpolate’, or ‘Specify value’ buttons below.\r\n\nAlternatively, you may select a range of data to modify by holding the shift key, and click-dragging the mouse over a range of data on the graph to the right, at any time."; }
        }

        public int DefaultReasonNumber
        {
            get { return 9; }
        }
    }

    public delegate void Updated();
}
