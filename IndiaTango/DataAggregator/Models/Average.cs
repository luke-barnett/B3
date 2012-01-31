using System;
using System.Collections.Generic;

namespace DataAggregator.Models
{
    class Average : IAggregationMethod
    {
        public string Name
        {
            get { return "Average"; }
        }

        public override string ToString()
        {
            return Name;
        }

        public float Aggregate(IEnumerable<float> values)
        {
            throw new NotImplementedException();
        }
    }
}
