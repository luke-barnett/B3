using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Detects missing values
    /// </summary>
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

        public string Abbreviation
        {
            get { return "MV"; }
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            Debug.Print("[Missing Values][{0}] Checking for missing values", sensorToCheck);

            var detectedValues = new List<ErroneousValue>();
            
            for (var time = sensorToCheck.Owner.StartYear.AddYears(sensorToCheck.Owner.LowestYearLoaded); time <= sensorToCheck.Owner.StartYear.AddYears(sensorToCheck.Owner.HighestYearLoaded + 1); time = time.AddMinutes(sensorToCheck.Owner.DataInterval))
            {
                if (!sensorToCheck.CurrentState.Values.ContainsKey(time))
                {
                    detectedValues.Add(new ErroneousValue(time, this, sensorToCheck));
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
                //wrapperGrid.Children.Add(new TextBlock { Text = "No Settings" });
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

        public List<LineSeries> GraphableSeries(DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
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
            get { return "This method is used to detect timestamps where there are missing data for the parameters you have selected for display on the graph to the right.\r\n\nIf you tick ‘Check detection methods for values’, timestamps with missing data will be displayed in the ‘Detected values’ list . Values will be detected only within the date range displayed on the graph to the right. You can select any combination of the detected values to modify using the ‘Delete’ (shortcut ‘Delete’ key), ‘Interpolate’, or ‘Specify value’ buttons below.\r\n\nAlternatively, you may select a range of data to modify by holding the shift key, and click-dragging the mouse over a range of data on the graph to the right, at any time."; }
        }

        public int DefaultReasonNumber
        {
            get { return 10; }
        }
    }
}
