using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    public class MinMaxRateOfChangeDetector : IDetectionMethod
    {
        private readonly AboveMaxValueDetector _aboveMaxValue;
        private readonly BelowMinValueDetector _belowMinValue;
        private readonly ToHighRateOfChangeDetector _highRateOfChange;
        private bool _showMaxMinLines;
        private Sensor _selectedSensor;

        private ComboBox _sensorsCombo;

        public event UpdateGraph GraphUpdateNeeded;

        public MinMaxRateOfChangeDetector()
        {
            _aboveMaxValue = new AboveMaxValueDetector(this);
            _belowMinValue = new BelowMinValueDetector(this);
            _highRateOfChange = new ToHighRateOfChangeDetector(this);
        }

        public string Name
        {
            get { return "Upper & Lower Limits + Rate of Change"; }
        }

        public string Abbreviation
        {
            get { return "Limits & RoC"; }
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            var detectedValues = new List<ErroneousValue>();

            var lastValue = new KeyValuePair<DateTime, float>();

            foreach (var value in sensorToCheck.CurrentState.Values)
            {
                if (value.Value < sensorToCheck.LowerLimit)
                    detectedValues.Add(new ErroneousValue(value.Key, _belowMinValue, sensorToCheck));
                else if (value.Value > sensorToCheck.UpperLimit)
                    detectedValues.Add(new ErroneousValue(value.Key, _aboveMaxValue, sensorToCheck));
                else if (Math.Abs(value.Value - lastValue.Value) > sensorToCheck.MaxRateOfChange)
                    detectedValues.Add(new ErroneousValue(value.Key, _highRateOfChange, sensorToCheck));
                lastValue = value;
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
                var wrapperGrid = new Grid();
                var stackPanel = new StackPanel();
                var checkBox = new CheckBox { Content = new TextBlock { Text = "Graph Upper and Lower Limits" }, IsChecked = _showMaxMinLines };
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

                graphOptions.Children.Add(new TextBlock { Text = "What sensor to use when graphing lines", Margin = new Thickness(0, 0, 10, 0) });

                _sensorsCombo = new ComboBox { Width = 100 };

                _sensorsCombo.SelectionChanged += (o, e) =>
                {
                    if (e.AddedItems.Count < 1)
                        return;

                    if (_selectedSensor != null)
                        _selectedSensor.PropertyChanged -= PropertyChangedInSelectedSensor;

                    _selectedSensor = e.AddedItems[0] as Sensor;

                    if (_selectedSensor != null)
                        _selectedSensor.PropertyChanged += PropertyChangedInSelectedSensor;



                    GraphUpdateNeeded();
                };

                graphOptions.Children.Add(_sensorsCombo);

                stackPanel.Children.Add(graphOptions);

                wrapperGrid.Children.Add(stackPanel);
                return wrapperGrid;
            }
        }

        private void PropertyChangedInSelectedSensor(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UpperLimit" || e.PropertyName == "LowerLimit")
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
            get { return new List<IDetectionMethod> { _aboveMaxValue, _belowMinValue, _highRateOfChange }; }
        }

        public override string ToString()
        {
            return string.Empty;
        }

        public bool IsEnabled { get; set; }

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
                else if (!_sensorsCombo.Items.Contains(_selectedSensor))
                    GraphUpdateNeeded();
            }
        }
    }

    public delegate void UpdateGraph();
}
