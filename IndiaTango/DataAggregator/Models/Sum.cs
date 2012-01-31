using System;
using System.Collections.Generic;

namespace DataAggregator.Models
{
    class Sum : IAggregationMethod
    {
        public string Name
        {
            get { return "Sum"; }
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
