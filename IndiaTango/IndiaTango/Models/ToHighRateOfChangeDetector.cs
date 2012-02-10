using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Detection method for values that are above the maximum rate of change for the sensor
    /// </summary>
    public class ToHighRateOfChangeDetector : IDetectionMethod
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

        public string About
        {
            get { return "This method is used to detect timestamps where there are unexpected changes from one timestamp to the next, for the parameters you have selected for display on the graph to the right.\r\n\nA maximum rate of change can be specified in the above metadata for each sensor. If you tick ‘Check detection methods for values’ ,timestamps where the rate of change exceeds this maximum rate will be displayed in the ‘Detected values’ list . Values will be detected only within the date range displayed on the graph to the right. You can select any combination of the timestamps from thedetected values list, and modify them using the ‘Delete’ (shortcut ‘Delete’ key), ‘Interpolate’, or ‘Specify value’ buttons below.\r\n\nAlternatively, you may select a range of data to modify by holding the shift key, and click-dragging the mouse over a range of data on the graph to the right, at any time."; }
        }

        public int DefaultReasonNumber
        {
            get { return 8; }
        }
    }
}
