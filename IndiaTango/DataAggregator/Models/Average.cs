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

        public float Aggregate(IEnumerable<KeyValuePair<DateTime, float>> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp, DateTime midPointTimestamp)
        {
            return !values.Any() ? float.NaN : values.Average(x => x.Value);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
