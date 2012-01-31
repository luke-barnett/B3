using System;
using System.Collections.Generic;

namespace DataAggregator.Models
{
    public class Series
    {
        public string Name { get; set; }

        public Dictionary<DateTime, float> Values;

        public Dictionary<DateTime, float> AggregatedValues;

        public AggregationModel AggregationModel { get; set; }

        public int ColumnIndex;

        public Series(string name)
        {
            Name = name;
            Values = new Dictionary<DateTime, float>();
            AggregatedValues = new Dictionary<DateTime, float>();
            AggregationModel = new AggregationModel();
        }
    }
}
