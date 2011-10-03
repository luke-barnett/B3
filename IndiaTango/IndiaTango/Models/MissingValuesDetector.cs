using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;

namespace IndiaTango.Models
{
    public class MissingValuesDetector : IDetectionMethod
    {
        public override string ToString()
        {
            return "Missing Values";
        }

        public string Name
        {
            get { return ToString(); }
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
    }
}
