using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class SensorTemplate
    {
        private MatchStyle _matchStyle;
        private string _pattern;
        private float _upperLimit = 0;
        private float _lowerLimit = 0;
        private float _maxRateOfChange = 0;
        private string _unit = "";

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

            _unit = unit;
            _upperLimit = upperLimit;
            _lowerLimit = lowerLimit;
            _maxRateOfChange = maxRateOfChange;

            _matchStyle = matchStyle;
            _pattern = pattern;
        }

        public string Unit
        {
            get { return _unit; }
        }

        public float UpperLimit
        {
            get { return _upperLimit; }
        }

        public float LowerLimit
        {
            get { return _lowerLimit; }
        }

        public float MaximumRateOfChange
        {
            get { return _maxRateOfChange; }
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
    }
}
