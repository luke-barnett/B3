using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Dummy detector to show a value above the maximum value of a sensor
    /// </summary>
    public class AboveMaxValueDetector : IDetectionMethod
    {
        private readonly MinMaxDetector _owner;

        public AboveMaxValueDetector(MinMaxDetector owner)
        {
            _owner = owner;
        }

        public string Name
        {
            get { return "Above Max"; }
        }

        public string Abbreviation
        {
            get { return ""; }
        }

        public IDetectionMethod This
        {
            get { return _owner; }
        }

        public List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck)
        {
            return _owner.GetDetectedValues(sensorToCheck);
        }

        public bool HasSettings
        {
            get { return _owner.HasSettings; }
        }

        public Grid SettingsGrid
        {
            get { return _owner.SettingsGrid; }
        }

        public bool HasGraphableSeries
        {
            get { return _owner.HasGraphableSeries; }
        }

        public bool CheckIndividualValue(Sensor sensor, DateTime timeStamp)
        {
            return _owner.CheckIndividualValue(sensor, timeStamp);
        }

        public List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate)
        {
            return _owner.GraphableSeries(sensorToBaseOn, startDate, endDate);
        }

        public List<LineSeries> GraphableSeries(DateTime startDate, DateTime endDate)
        {
            return _owner.GraphableSeries(startDate, endDate);
        }

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod>(); }
        }

        public bool IsEnabled { get; set; }

        public ListBox ListBox { get; set; }

        public Sensor[] SensorOptions
        {
            set
            {
                if (value == null) throw new ArgumentNullException("value");
            }
        }

        public string About
        {
            get { return "I'm a fake detection method used to make things look pretty"; }
        }

        public int DefaultReasonNumber
        {
            get { return 2; }
        }
    }
}
