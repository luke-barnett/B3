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

        public float Aggregate(IEnumerable<KeyValuePair<DateTime, float>> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp, DateTime midPointTimestamp)
        {
            return !values.Any() ? float.NaN : values.Sum(x => x.Value);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
