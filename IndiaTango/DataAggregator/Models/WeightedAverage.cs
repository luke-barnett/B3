using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAggregator.Models
{
    class WeightedAverage : IAggregationMethod
    {
        public string Name
        {
            get { return "Weighted Average"; }
        }

        public float Aggregate(IEnumerable<KeyValuePair<DateTime, float>> values, DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp, DateTime midPointTimestamp)
        {
            if (!values.Any())
                return float.NaN;

            var valuesArray = values.ToArray();
            var valuesWeightingArray = new float[valuesArray.Length];

            valuesWeightingArray[0] = (valuesArray[0].Key - inclusiveStartTimestamp).Ticks;

            for (var i = 1; i < valuesArray.Length; i++)
            {
                valuesWeightingArray[i] = (valuesArray[i].Key - valuesArray[i - 1].Key).Ticks;
            }

            var weightingTotal = valuesWeightingArray.Sum();

            var average = 0d;

            for (var i = 0; i < valuesArray.Length; i++)
            {
                average += valuesArray[i].Value * (valuesWeightingArray[i] / weightingTotal);
            }

            return (float)average;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
