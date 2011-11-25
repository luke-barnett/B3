using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    internal class ToHighRateOfChangeDetector : IDetectionMethod
    {
        public string Name
        {
            get { return "Rate of Change"; }
        }

        public string Abbreviation
        {
            get { return "RoC"; }
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
                if (Math.Abs(value.Value - lastValue.Value) > sensorToCheck.MaxRateOfChange)
                    detectedValues.Add(new ErroneousValue(value.Key, this, sensorToCheck));
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
            get { return new Grid(); }
        }

        public bool HasGraphableSeries
        {
            get { return false; }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            if (sensor.CurrentState.Values.ContainsKey(timeStamp))
                return false;
            var value = sensor.CurrentState.Values[timeStamp];
            return Math.Abs(value - sensor.CurrentState.Values[sensor.CurrentState.FindPrevValue(timeStamp)]) > sensor.MaxRateOfChange;
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

        public bool IsEnabled { get; set; }

        public ListBox ListBox { get; set; }

        public Sensor[] SensorOptions
        {
            set { return; }
        }
    }
}
