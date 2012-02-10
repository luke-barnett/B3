using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Detects values that are below or above the limits for it's sensor
    /// </summary>
    public class MinMaxDetector : IDetectionMethod
    {
        private readonly AboveMaxValueDetector _aboveMaxValue;
        private readonly BelowMinValueDetector _belowMinValue;
        private bool _showMaxMinLines;
        private Sensor _selectedSensor;
        private bool _isEnabled;

        private ComboBox _sensorsCombo;

        private Grid _settings;

        public event UpdateGraph GraphUpdateNeeded;

        public MinMaxDetector()
        {
            _aboveMaxValue = new AboveMaxValueDetector(this);
            _belowMinValue = new BelowMinValueDetector(this);
        }

        public string Name
        {
            get { return "Upper & Lower Limits"; }
        }

        public string Abbreviation
        {
            get { return "Limits"; }
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            var detectedValues = new List<ErroneousValue>();

            foreach (var value in sensorToCheck.CurrentState.Values)
            {
                if (value.Value < sensorToCheck.LowerLimit)
                    detectedValues.Add(new ErroneousValue(value.Key, _belowMinValue, sensorToCheck));
                else if (value.Value > sensorToCheck.UpperLimit)
                    detectedValues.Add(new ErroneousValue(value.Key, _aboveMaxValue, sensorToCheck));
            }

            return detectedValues;
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
                    var wrapperGrid = new Grid();
                    var stackPanel = new StackPanel();
                    var checkBox = new CheckBox
                                       {
                                           Content = new TextBlock { Text = "Graph Upper and Lower Limits" },
                                           IsChecked = _showMaxMinLines
                                       };
                    checkBox.Checked += (o, e) =>
                                            {
                                                _showMaxMinLines = true;
                                                GraphUpdateNeeded();
                                            };
                    checkBox.Unchecked += (o, e) =>
                                              {
                                                  _showMaxMinLines = false;
                                                  GraphUpdateNeeded();
                                              };

                    stackPanel.Children.Add(checkBox);

                    var graphOptions = new StackPanel { Orientation = Orientation.Horizontal };

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

                                                              if (_selectedSensor != null)
                                                                  _selectedSensor.PropertyChanged -=
                                                                      PropertyChangedInSelectedSensor;

                                                              _selectedSensor = e.AddedItems[0] as Sensor;

                                                              if (_selectedSensor != null)
                                                                  _selectedSensor.PropertyChanged +=
                                                                      PropertyChangedInSelectedSensor;

                                                              if (_showMaxMinLines)
                                                                  GraphUpdateNeeded();
                                                          };

                    graphOptions.Children.Add(_sensorsCombo);

                    stackPanel.Children.Add(graphOptions);

                    wrapperGrid.Children.Add(stackPanel);
                    _settings = wrapperGrid;
                }
                return _settings;
            }
        }

        private void PropertyChangedInSelectedSensor(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == "UpperLimit" || e.PropertyName == "LowerLimit") && _showMaxMinLines)
                GraphUpdateNeeded();
        }

        public bool HasGraphableSeries
        {
            get { return _showMaxMinLines; }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            if (sensor.CurrentState.Values.ContainsKey(timeStamp))
                return false;
            var value = sensor.CurrentState.Values[timeStamp];
            return value > sensor.UpperLimit || value < sensor.LowerLimit || Math.Abs(value - sensor.CurrentState.Values[sensor.CurrentState.FindPrevValue(timeStamp)]) > sensor.MaxRateOfChange;
        }

        public List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate)
        {
            return new List<LineSeries> { new LineSeries { DataSeries = new DataSeries<DateTime, float>("Upper Limit") { new DataPoint<DateTime, float>(startDate, sensorToBaseOn.UpperLimit), new DataPoint<DateTime, float>(endDate, sensorToBaseOn.UpperLimit) }, LineStroke = Brushes.OrangeRed }, new LineSeries { DataSeries = new DataSeries<DateTime, float>("Lower Limit") { new DataPoint<DateTime, float>(startDate, sensorToBaseOn.LowerLimit), new DataPoint<DateTime, float>(endDate, sensorToBaseOn.LowerLimit) }, LineStroke = Brushes.OrangeRed } };
        }

        public List<LineSeries> GraphableSeries(DateTime startDate, DateTime endDate)
        {
            return (_sensorsCombo.SelectedIndex == -1) ? new List<LineSeries>() : new List<LineSeries> { new LineSeries { DataSeries = new DataSeries<DateTime, float>("Upper Limit") { new DataPoint<DateTime, float>(startDate, _selectedSensor.UpperLimit), new DataPoint<DateTime, float>(endDate, _selectedSensor.UpperLimit) }, LineStroke = Brushes.OrangeRed }, new LineSeries { DataSeries = new DataSeries<DateTime, float>("Lower Limit") { new DataPoint<DateTime, float>(startDate, _selectedSensor.LowerLimit), new DataPoint<DateTime, float>(endDate, _selectedSensor.LowerLimit) }, LineStroke = Brushes.OrangeRed } };
        }

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod> { _aboveMaxValue, _belowMinValue }; }
        }

        public override string ToString()
        {
            return string.Empty;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                if (IsEnabled && _showMaxMinLines)
                    GraphUpdateNeeded();
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
                else if (_selectedSensor != null && !_sensorsCombo.Items.Contains(_selectedSensor) && _showMaxMinLines)
                    GraphUpdateNeeded();
            }
        }

        public string About
        {
            get { return "This method is used to detect timestamps where data are outside the bounds of what might be reasonably expected for the parameters you have selected for display on the graph to the right.\r\n\nExpected minimum and maximum values can be specified in the above metadata for each sensor. Select ‘Graph Upper and Lower Limits’ and select which sensor you wish to edit, in order to display the Minimum and Maximum boundary on the graph to the right. If you tick ‘Check detection methods for values’, timestamps for which data lie outside these bounds will be displayed in the ‘Detected values’ list . Values will be detected only within the date range displayed on the graph to the right. You can select any combination of the timestamps from the detected values list, andmodify them using the ‘Delete’ (shortcut ‘Delete’ key), ‘Interpolate’, or ‘Specify value’ buttons below.\r\n\nAlternatively, you may select a range of data to modify by holding the shift key, and click-dragging the mouse over a range of data on the graph to the right, at any time."; }
        }

        public int DefaultReasonNumber
        {
            get { return 2; }
        }
    }

    public delegate void UpdateGraph();
}
