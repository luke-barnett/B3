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
    class RunningMeanStandardDeviationDetector : IDetectionMethod
    {
        private int _smoothingPeriod = 60;
        private int _requestedSmoothingPeriod = 60;
        private float _numberOfStandardDeviations = 1;
        private float _requestedNumerOfStandardDeviations = 1;
        private Dictionary<DateTime, float> _upperLine = new Dictionary<DateTime, float>();
        private Dictionary<DateTime, float> _lowerLine = new Dictionary<DateTime, float>();
        private bool _showGraph;

        public event UpdateGraph GraphUpdateNeeded;

        public event Updated RefreshDetectedValues;

        private Sensor _currentSensor;

        public override string ToString()
        {
            return string.Empty;
        }

        public string Name
        {
            get { return "Running Mean with Standard Deviation"; }
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
                         select new ErroneousValue(value.Key, this)).ToList();
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
                var wrapper = new Grid();
                var stackPanel = new StackPanel();

                var graphGrid = new Grid();
                var graphCheckBox = new CheckBox { Content = new TextBlock { Text = "Show Graph" }, HorizontalAlignment = HorizontalAlignment.Left, IsChecked = _showGraph};

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

                var updateGraphButton = new Button { Content = new TextBlock { Text = "Update Graph" }, HorizontalAlignment = HorizontalAlignment.Right };

                updateGraphButton.Click += (o, e) => GraphUpdateNeeded();

                graphGrid.Children.Add(updateGraphButton);

                stackPanel.Children.Add(graphGrid);

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

                var smoothingPeriodSlider = new Slider { Value = _requestedSmoothingPeriod / 60, Maximum = 60, Minimum = 1 };
                smoothingPeriodSlider.ValueChanged += (o, e) =>
                                                        {
                                                            _requestedSmoothingPeriod = (int)e.NewValue * 60;
                                                            smoothingPeriodHoursText.Text = _requestedSmoothingPeriod / 60 + " Hour(s)";
                                                        };
                smoothingPeriodSlider.PreviewMouseUp += (o, e) =>
                                                               {
                                                                   if (_requestedSmoothingPeriod != _smoothingPeriod)
                                                                   {
                                                                       _smoothingPeriod = _requestedSmoothingPeriod;
                                                                       GenerateUpperAndLowerLines(_currentSensor);
                                                                       Debug.WriteLine("Refresh of values needed");
                                                                       RefreshDetectedValues();
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

                var standarDeviationsSlider = new Slider { Value = _requestedNumerOfStandardDeviations, Maximum = 5, Minimum = 0, TickFrequency = 0.5, TickPlacement = TickPlacement.BottomRight };
                standarDeviationsSlider.ValueChanged += (o, e) =>
                                                            {
                                                                _requestedNumerOfStandardDeviations = (float)e.NewValue;
                                                                standardDeviationsText.Text =
                                                                    _requestedNumerOfStandardDeviations.ToString();
                                                            };
                standarDeviationsSlider.PreviewMouseUp += (o, e) =>
                                                                 {
                                                                     if (Math.Abs(_requestedNumerOfStandardDeviations - _numberOfStandardDeviations) > 0.01)
                                                                     {
                                                                         _numberOfStandardDeviations = _requestedNumerOfStandardDeviations;
                                                                         GenerateUpperAndLowerLines(_currentSensor);
                                                                         Debug.WriteLine("Refresh of values needed");
                                                                         RefreshDetectedValues();
                                                                         GraphUpdateNeeded();
                                                                     }
                                                                 };
                Grid.SetRow(standarDeviationsSlider, 1);
                standarDeviationsGrid.Children.Add(standarDeviationsSlider);

                stackPanel.Children.Add(standarDeviationsGrid);

                wrapper.Children.Add(stackPanel);
                return wrapper;
            }
        }

        public bool HasGraphableSeries
        {
            get { return (_showGraph && _upperLine.Count != 0 && _lowerLine.Count != 0); }
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

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod>(); }
        }

        private void GenerateUpperAndLowerLines(Sensor sensor)
        {
            //Set Current Sensor
            _currentSensor = sensor;

            if(_currentSensor == null)
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
                    if(_currentSensor.CurrentState.Values.ContainsKey(i))
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
    }

    public delegate void Updated();
}
