using System;

namespace IndiaTango.Models
{
    /// <summary>
    /// A Sensor wrapper used with lists
    /// </summary>
    public class ListedSensor
    {
        private Sensor _sensor;
        private Dataset _ds;

        public ListedSensor(Sensor s, Dataset d)
        {
            if(s == null)
                throw new ArgumentNullException("The specified sensor cannot be null.");

            if (d == null)
                throw new ArgumentNullException("The specified dataset cannot be null.");

            _sensor = s;
            _ds = d;
        }

        public Sensor Sensor
        {
            get { return _sensor; }
            set
            {
                if(value == null)
                    throw new ArgumentNullException("The specified sensor cannot be null.");

                _sensor = value;
            }
        }

        public Dataset Dataset
        {
            get { return _ds; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("The specified dataset cannot be null.");

                _ds = value;
            }
        }

        public bool IsFailing
        {
            get { return _sensor.IsFailing(_ds); }
        }

        public override string ToString()
        {
            return Sensor.ToString();
        }
    }
}
