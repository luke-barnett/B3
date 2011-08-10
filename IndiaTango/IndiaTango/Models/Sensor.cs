using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class Sensor
    {
        private string _name;
        private string _unit;

        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, string manufacturer)
        {
            if (name == "") throw new ArgumentNullException("Sensor name cannot be empty.");
            _name = name;
            Description = description;
            UpperLimit = upperLimit;
            LowerLimit = lowerLimit;
            if (unit == "") throw new ArgumentNullException("Sensor Unit cannot be empty.");
            _unit = unit;
            MaxRateOfChange = maxRateOfChange;
            Manufacturer = manufacturer;
        }

        public string Name
        {
            get { return _name; }
            set 
            {
                if (value == "") throw new FormatException("Sensor name cannot be empty.");
                _name = value; 
            }
        }

        public string Description { get; set; }

        public float UpperLimit { get; set; }

        public float LowerLimit { get; set; }

        public string Unit
        {
            get { return _unit; }
            set
            {
                if(value == "") throw new FormatException("Sensor Unit cannot be empty.");
                _unit = value;
            }
        }

        public float MaxRateOfChange { get; set; }

        public string Manufacturer { get; set; }
    }
}
