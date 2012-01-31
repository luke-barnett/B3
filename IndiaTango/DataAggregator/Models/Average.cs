using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAggregator.Models
{
    class Average : IAggregationMethod
    {
        public string Name
        {
            get { return "Average"; }
        }

        public float Aggregate(IEnumerable<float> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp)
        {
            return values.Average();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
