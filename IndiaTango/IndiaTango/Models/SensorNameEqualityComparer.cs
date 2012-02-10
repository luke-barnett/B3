using System.Collections.Generic;

namespace IndiaTango.Models
{
    /// <summary>
    /// Compares the equality of two sensors based on their hash codes
    /// </summary>
    public class SensorNameEqualityComparer : IEqualityComparer<Sensor>
    {
        public bool Equals(Sensor x, Sensor y)
        {
            return GetHashCode(x) == GetHashCode(y);
        }

        public int GetHashCode(Sensor obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}

