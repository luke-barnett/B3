using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class SensorTemplate
    {
        private Sensor _sensor;
        private MatchStyle _matchStyle;
        private string _pattern;

        public enum MatchStyle
        {
            Contains,
            StartsWith,
            EndsWith
        };

        public SensorTemplate(Sensor sensor, MatchStyle matchStyle, string pattern)
        {
            if(sensor == null)
                throw new ArgumentNullException("You must provide a sensor with the desired values for this template.");

            if(pattern == "")
                throw new ArgumentException("The pattern to match this sensor on cannot be null.");
            
            _sensor = sensor;
            _matchStyle = matchStyle;
            _pattern = pattern;
        }

        public string Unit
        {
            get { return _sensor.Unit; }
        }

        public float UpperLimit
        {
            get { return _sensor.UpperLimit; }
        }

        public float LowerLimit
        {
            get { return _sensor.LowerLimit; }
        }

        public float MaximumRateOfChange
        {
            get { return _sensor.MaxRateOfChange; }
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
