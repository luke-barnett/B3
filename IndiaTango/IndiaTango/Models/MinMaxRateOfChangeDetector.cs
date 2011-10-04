using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class MinMaxRateOfChangeDetector : IDetectionMethod
    {
        private AboveMaxValueDetector _aboveMaxValue;
        private BelowMinValueDetector _belowMinValue;
        private ToHighRateOfChangeDetector _highRateOfChange;

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
                    detectedValues.Add(new ErroneousValue(value.Key, _belowMinValue));
                else if (value.Value > sensorToCheck.UpperLimit)
                    detectedValues.Add(new ErroneousValue(value.Key, _aboveMaxValue));
                else if (Math.Abs(value.Value - lastValue.Value) > sensorToCheck.MaxRateOfChange)
                    detectedValues.Add(new ErroneousValue(value.Key, _highRateOfChange));
                lastValue = value;
            }

            return detectedValues;
        }

        public bool HasSettings
        {
            get { return false; }
        }

        public Grid SettingsGrid
        {
            get
            {
                var wrapperGrid = new Grid();
                wrapperGrid.Children.Add(new TextBlock { Text = "No Settings" });
                return wrapperGrid;
            }
        }

        public bool HasGraphableSeries
        {
            get { return true; }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            var value = sensor.CurrentState.Values[timeStamp];
            if (value > sensor.UpperLimit || value < sensor.LowerLimit)
                return true;
            //TODO: Check rate of change
            return false;
        }

        public List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate)
        {
            return new List<LineSeries> { new LineSeries { DataSeries = new DataSeries<DateTime, float>("Upper Limit") { new DataPoint<DateTime, float>(startDate, sensorToBaseOn.UpperLimit), new DataPoint<DateTime, float>(endDate, sensorToBaseOn.UpperLimit) }, LineStroke = Brushes.OrangeRed }, new LineSeries { DataSeries = new DataSeries<DateTime, float>("Lower Limit") { new DataPoint<DateTime, float>(startDate, sensorToBaseOn.LowerLimit), new DataPoint<DateTime, float>(endDate, sensorToBaseOn.LowerLimit) }, LineStroke = Brushes.OrangeRed } };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
