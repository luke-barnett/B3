using System;
using System.Collections.Generic;

namespace DataAggregator.Models
{
    public interface IAggregationMethod
    {
        string Name { get; }

        float Aggregate(IEnumerable<float> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp);
    }
}
