using System;
using System.Collections.Generic;
using System.Linq;
namespace DataAggregator.Models
{
    class FirstValue : IAggregationMethod
    {
        public string Name
        {
            get { return "First Value"; }
        }

        public float Aggregate(IEnumerable<KeyValuePair<DateTime, float>> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp)
        {
            return !values.Any() ? float.NaN : values.First().Value;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
