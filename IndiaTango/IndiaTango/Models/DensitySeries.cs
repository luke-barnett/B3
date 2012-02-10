using System;
using System.Collections.Generic;

namespace IndiaTango.Models
{
    /// <summary>
    /// Object to describe a series of densities
    /// </summary>
    public class DensitySeries
    {
        private readonly float _depth;
        private readonly Dictionary<DateTime, double> _density;

        public DensitySeries(float depth)
        {
            _depth = depth;
            _density = new Dictionary<DateTime, double>();
        }

        public float Depth { get { return _depth; } }

        public Dictionary<DateTime, double> Density { get { return _density; } }

        public void AddValue(DateTime timestamp, double value)
        {
            _density[timestamp] = value;
        }
    }
}
