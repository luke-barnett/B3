using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class RepeatedValuesDetector : IDetectionMethod
    {
        private int _requiredNumberInSequence = 100;
        private int _requestedNumberInSequence = 100;
        private Grid _settingsGrid;

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
            var detectedValues = new Dictionary<DateTime, ErroneousValue>();

            var queue = new Queue<KeyValuePair<DateTime, float>>();

            foreach (var keyValuePair in sensorToCheck.CurrentState.Values.OrderBy(x => x.Key))
            {
                //First add to the queue
                queue.Enqueue(keyValuePair);
                //If it is too big make it smaller
                while (queue.Count > _requiredNumberInSequence)
                    queue.Dequeue();
                if (queue.Count == _requiredNumberInSequence)
                {
                    var queueArray = queue.ToArray();
                    var allTheSame = true;
                    for (var i = 1; i < queue.Count; i++)
                    {
                        var same = queueArray[i].Value.ToString().CompareTo(queueArray[0].Value.ToString()) == 0;

                        if (!same)
                            allTheSame = false;
                    }
                    if (allTheSame)
                    {
                        foreach (var valuePair in queue)
                        {
                            detectedValues[valuePair.Key] = new ErroneousValue(valuePair.Key, valuePair.Value, sensorToCheck);
                        }
                    }
                }
            }

            return detectedValues.Select(x => x.Value).OrderBy(x => x.TimeStamp).ToList();
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

                    var label = new TextBlock { Text = "Number of sequential data values to look for:" };
                    Grid.SetRow(label, 0);

                    grid.Children.Add(label);

                    var slider = new Slider { Minimum = 2, Maximum = 100, Value = 100, TickFrequency = 98 };

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

                    Grid.SetRow(sliderGrid, 1);
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

        public void OnRefreshDetectedValues()
        {
            if (RefreshDetectedValues != null)
                RefreshDetectedValues();
        }
    }
}
