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

        public override string ToString()
        {
            return Name;
        }

        public float Aggregate(IEnumerable<float> values)
        {
            return !values.Any() ? float.NaN : values.First();
        }
    }
}
