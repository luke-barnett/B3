using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class BelowMinValueDetector : IDetectionMethod
    {
        private readonly MinMaxRateOfChangeDetector _owner;

        public BelowMinValueDetector(MinMaxRateOfChangeDetector owner)
        {
            _owner = owner;
        }

        public string Name
        {
            get { return "Below Min"; }
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

        public List<IDetectionMethod> Children
        {
            get { return new List<IDetectionMethod>(); }
        }

        public bool IsEnabled { get; set; }

        public ListBox ListBox { get; set; }
    }
}
