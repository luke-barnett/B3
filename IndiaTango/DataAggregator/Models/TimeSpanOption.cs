using System;

namespace DataAggregator.Models
{
    public class TimeSpanOption
    {
        public TimeSpan TimeSpan;
        public String Option;

        public override string ToString()
        {
            return Option;
        }
    }
}
