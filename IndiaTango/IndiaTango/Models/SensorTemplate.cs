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
        private SummaryType _sType;

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

        public SensorTemplate(string unit, float upperLimit, float lowerLimit, float maxRateOfChange, MatchStyle matchStyle, string pattern):this(unit,upperLimit,lowerLimit,maxRateOfChange,matchStyle,pattern,SummaryType.Average){}


        public SensorTemplate(string unit, float upperLimit, float lowerLimit, float maxRateOfChange, MatchStyle matchStyle, string pattern,SummaryType sType)
        {
            if (String.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("You must a non-blank default unit.");

            if (pattern == null)
                throw new ArgumentException("The pattern to match a sensor on cannot be blank.");

            if (lowerLimit > upperLimit)
                throw new ArgumentOutOfRangeException(
                    "The lower limit must be less than the upper limit for this preset.");

            _unit = unit;
            _upperLimit = upperLimit;
            _lowerLimit = lowerLimit;
            _maxRateOfChange = maxRateOfChange;
            _sType = sType;

            _matchStyle = matchStyle;
            _pattern = pattern;
        }

        [DataMember]
        public string Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }

        public SummaryType SummaryType
        {
            get { return _sType; }
            set { _sType = value; }
        }

        /*
         * This is a side-effect of serialisation.
         * Serialisation alphabetises members. Unfortunately, LowerLimit comes before UpperLimit, so when we de-serialise,
         * we set LowerLimit before UpperLimit, which throws an ArgumentOutOfRangeException.
         */
        [DataMember(Name="AUpperLimit")]
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
        public string Pattern
        {
            get { return _pattern; }
            set
            {
                if (value == null)
                    throw new ArgumentException("The pattern to match a sensor on cannot be blank.");
                
                _pattern = value;
            }
        }

        [DataMember]
        public MatchStyle MatchingStyle
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
            sensor.SummaryType = SummaryType;
        }

        public static void ExportAll(List<SensorTemplate> templates)
        {
            // TODO: check all items != null and list != null?
            var dcs = new DataContractSerializer(typeof (List<SensorTemplate>));
            var fs = new FileStream(ExportPath, FileMode.Create, FileAccess.ReadWrite);
            dcs.WriteObject(fs, templates);
            fs.Close();
        }

        public static List<SensorTemplate> ImportAll()
        {
            if(!File.Exists(ExportPath))
                return new List<SensorTemplate>(); // File may not exist yet because there are no serialised presets.

            var dcs = new DataContractSerializer(typeof (List<SensorTemplate>));
            var fs = new FileStream(ExportPath, FileMode.Open, FileAccess.Read);
            var result = (List<SensorTemplate>)dcs.ReadObject(fs);
            fs.Close();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is SensorTemplate)
            {
                var o = obj as SensorTemplate;
                return o.LowerLimit == LowerLimit && o.UpperLimit == UpperLimit && o.MatchingStyle == MatchingStyle &&
                       o.MaximumRateOfChange == MaximumRateOfChange && o.Pattern == Pattern;
            }
            else
                return false;
        }

        public override string ToString()
        {
            var ret = "Match if ";

            switch(MatchingStyle)
            {
                case MatchStyle.Contains:
                    ret += "contains";
                    break;
                case MatchStyle.StartsWith:
                    ret += "starts with";
                    break;
                case MatchStyle.EndsWith:
                    ret += "ends with";
                    break;
            }

            ret += " '" + Pattern + "'";

            return ret;
        }
    }
}
