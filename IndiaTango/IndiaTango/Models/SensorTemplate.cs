using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace IndiaTango.Models
{
    [DataContract]
    public class SensorTemplate
    {
        private MatchStyle _matchStyle;
        private string _pattern;
        private float _upperLimit = 0;
        private float _lowerLimit = 0;
        private float _maxRateOfChange = 0;
        private string _unit = "";

        public static string ExportPath
        {
            get { return Path.Combine(Common.AppDataPath, "ExportedSensorPresets.xml"); }
        }

        public enum MatchStyle
        {
            Contains,
            StartsWith,
            EndsWith
        };

        public SensorTemplate(string unit, float upperLimit, float lowerLimit, float maxRateOfChange, MatchStyle matchStyle, string pattern)
        {
            if(String.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("You must a non-null, non-empty default unit.");

            if(pattern == null)
                throw new ArgumentException("The pattern to match this sensor on cannot be null.");

            if(lowerLimit > upperLimit)
                throw new ArgumentOutOfRangeException("The lower limit must be less than the upper limit for this sensor template.");

            _unit = unit;
            _upperLimit = upperLimit;
            _lowerLimit = lowerLimit;
            _maxRateOfChange = maxRateOfChange;

            _matchStyle = matchStyle;
            _pattern = pattern;
        }

        [DataMember]
        public string Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }

        [DataMember]
        public float UpperLimit
        {
            get { return _upperLimit; }
            set
            {
                if(value < LowerLimit)
                    throw new ArgumentOutOfRangeException("Upper limit must be greater than lower limit.");

                _upperLimit = value;
            }
        }

        [DataMember]
        public float LowerLimit
        {
            get { return _lowerLimit; }
            set
            {
                if(value > UpperLimit)
                    throw new ArgumentOutOfRangeException("Lower limit cannot be greater than upper limit.");

                _lowerLimit = value;
            }
        }

        [DataMember]
        public float MaximumRateOfChange
        {
            get { return _maxRateOfChange; }
            set { _maxRateOfChange = value; }
        }

        // Properties used for serialisation only
        [DataMember]
        private string Pattern
        {
            get { return _pattern; }
            set { _pattern = value; }
        }

        [DataMember]
        private MatchStyle MatchingStyle
        {
            get { return _matchStyle; }
            set { _matchStyle = value; }
        }
        public bool Matches(Sensor testSensor)
        {
            if (_matchStyle == MatchStyle.Contains)
                return testSensor.Name.Contains(_pattern);
            else if (_matchStyle == MatchStyle.StartsWith)
                return testSensor.Name.StartsWith(_pattern);
            else
                return testSensor.Name.EndsWith(_pattern);
        }

        public void ProvideDefaultValues(Sensor sensor)
        {
            if (sensor == null)
                throw new ArgumentNullException("", "Cannot assign template values to a null sensor.");

            sensor.Unit = Unit;
            sensor.UpperLimit = UpperLimit;
            sensor.LowerLimit = LowerLimit;
            sensor.MaxRateOfChange = MaximumRateOfChange;
        }

        public static void ExportAll(List<SensorTemplate> templates)
        {
            // TODO: check all items != null and list != null?
            var dcs = new DataContractSerializer(typeof (List<SensorTemplate>));
            var fs = new FileStream(ExportPath, FileMode.Create, FileAccess.ReadWrite);
            dcs.WriteObject(fs, templates);
            fs.Close();
        }
    }
}
