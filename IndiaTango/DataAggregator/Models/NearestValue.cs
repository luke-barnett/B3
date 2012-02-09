using System;
using System.Collections.Generic;
using System.Linq;
namespace DataAggregator.Models
{
    class NearestValue : IAggregationMethod
    {
        public string Name
        {
            get { return "Nearest Timestamp"; }
        }

        public float Aggregate(IEnumerable<KeyValuePair<DateTime, float>> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp, DateTime midPointTimestamp)
        {
            if (!values.Any())
                return float.NaN;

            var distances = values.Select(x => new KeyValuePair<DateTime,TimeSpan>(x.Key,(midPointTimestamp - x.Key).Duration())).ToArray();

            var minDistance = distances[0];

            for(var i = 1; i < distances.Length; i++)
            {
                if(distances[i].Value < minDistance.Value)
                    minDistance = distances[i];
            }

            return values.DefaultIfEmpty(new KeyValuePair<DateTime, float>(DateTime.MinValue,float.NaN)).FirstOrDefault(x => x.Key == minDistance.Key).Value;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
