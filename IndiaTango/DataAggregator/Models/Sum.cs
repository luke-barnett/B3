using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAggregator.Models
{
    class Sum : IAggregationMethod
    {
        public string Name
        {
            get { return "Sum"; }
        }

        public float Aggregate(IEnumerable<float> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp)
        {
            return values.Sum();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
