using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    public class MissingValuesDetector : IDetectionMethod
    {
        public override string ToString()
        {
            return string.Empty;
        }

        public string Name
        {
            get { return "Missing Values"; }
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            Debug.WriteLine("Checking for missing values");

            var detectedValues = new List<ErroneousValue>();
            
            for (var time = sensorToCheck.Owner.StartTimeStamp; time <= sensorToCheck.Owner.EndTimeStamp; time = time.AddMinutes(sensorToCheck.Owner.DataInterval))
            {
                if (!sensorToCheck.CurrentState.Values.ContainsKey(time))
                {
                    detectedValues.Add(new ErroneousValue(time, this));
                }
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
            get { return false; }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            return !sensor.CurrentState.Values.ContainsKey(timeStamp);
        }

        public List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod>(); }
        }

        public bool IsEnabled { get; set; }
    }
}
