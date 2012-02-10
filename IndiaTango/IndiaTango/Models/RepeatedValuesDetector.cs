using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Detects repeated values
    /// </summary>
    public class RepeatedValuesDetector : IDetectionMethod
    {
        private int _requiredNumberInSequence = 100;
        private int _requestedNumberInSequence = 100;
        private Grid _settingsGrid;
        private bool _skipFirstValue;

        public event Updated RefreshDetectedValues;

        public string Name
        {
            get { return "Repeated Values"; }
        }

        public string Abbreviation
        {
            get { return "RV"; }
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            var detectedValues = new List<ErroneousValue>();

            var queue = new Queue<KeyValuePair<DateTime, float>>();

            var orderedValuesArray = sensorToCheck.CurrentState.Values.OrderBy(x => x.Key).ToArray();

            foreach (var value in orderedValuesArray)
            {
                queue.Enqueue(value);
                var allTheSame = String.CompareOrdinal(queue.Peek().Value.ToString(CultureInfo.InvariantCulture), value.Value.ToString(CultureInfo.InvariantCulture)) == 0;

                if (allTheSame) continue;

                var numberOfAllTheSame = queue.Count - 1;
                if (numberOfAllTheSame >= _requiredNumberInSequence)
                {
                    var listOfRepeatedValues = queue.DropLast();
                    detectedValues.AddRange(from valuePair in (_skipFirstValue) ? listOfRepeatedValues.Skip(1) : listOfRepeatedValues select new ErroneousValue(valuePair.Key, valuePair.Value, sensorToCheck));
                }
                queue.Clear();
                queue.Enqueue(value);
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
                if (_settingsGrid == null)
                {
                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                    var checkBox = new CheckBox
                                       {
                                           Content = "Skip first value",
                                           ToolTip =
                                               "When checked it will consider the first value in a repeated series to be a legitimate value and so won't add it to the list of erroneous values",
                                           Margin = new Thickness(0, 0, 0, 5)
                                       };

                    Grid.SetRow(checkBox, 0);
                    grid.Children.Add(checkBox);

                    checkBox.Checked += (o, e) =>
                                            {
                                                _skipFirstValue = true;
                                                OnRefreshDetectedValues();
                                            };

                    checkBox.Unchecked += (o, e) =>
                                              {
                                                  _skipFirstValue = false;
                                                  OnRefreshDetectedValues();
                                              };

                    var label = new TextBlock { Text = "Number of sequential data values to look for:" };
                    Grid.SetRow(label, 1);

                    grid.Children.Add(label);

                    var slider = new Slider { Minimum = 2, Maximum = 200, Value = 100, TickFrequency = 98 };

                    var valueLabel = new TextBlock { Text = ((int)slider.Value).ToString() };

                    slider.ValueChanged += (o, e) =>
                                               {
                                                   _requestedNumberInSequence = (int)slider.Value;
                                                   valueLabel.Text = _requestedNumberInSequence.ToString();
                                               };

                    slider.PreviewMouseUp += (o, e) =>
                                                 {
                                                     if (_requestedNumberInSequence == _requiredNumberInSequence)
                                                         return;
                                                     _requiredNumberInSequence = _requestedNumberInSequence;
                                                     OnRefreshDetectedValues();
                                                 };

                    var sliderGrid = new Grid();
                    sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                    Grid.SetColumn(slider, 0);
                    sliderGrid.Children.Add(slider);

                    Grid.SetColumn(valueLabel, 1);
                    sliderGrid.Children.Add(valueLabel);

                    Grid.SetRow(sliderGrid, 2);
                    grid.Children.Add(sliderGrid);
                    _settingsGrid = grid;
                }
                return _settingsGrid;
            }
        }

        public bool HasGraphableSeries
        {
            get { return false; }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            throw new NotImplementedException();
        }

        public List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate)
        {
            return new List<LineSeries>();
        }

        public List<LineSeries> GraphableSeries(DateTime startDate, DateTime endDate)
        {
            return new List<LineSeries>();
        }

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod>(); }
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public ListBox ListBox
        {
            get;
            set;
        }

        public Sensor[] SensorOptions
        {
            set { return; }
        }

        public string About
        {
            get { return "Checks for repeating sequential values"; }
        }

        public int DefaultReasonNumber
        {
            get { return 1; }
        }

        public void OnRefreshDetectedValues()
        {
            if (RefreshDetectedValues != null)
                RefreshDetectedValues();
        }
    }
}
