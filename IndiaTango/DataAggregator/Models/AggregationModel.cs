using System;
using System.ComponentModel;
using System.Linq;

namespace DataAggregator.Models
{

    public class AggregationModel
    {
        private IAggregationMethod _aggregationMethod = AggregationOptions[0];

        public IAggregationMethod AggregationMethod
        {
            get { return _aggregationMethod; }
            set
            {
                _aggregationMethod = value;
            }
        }

        public string AggregationMethodString
        {
            get { return _aggregationMethod.Name; }
            set { _aggregationMethod = AggregationOptions.First(x => x.Name == value); }
        }

        public string[] AggregationMethodStrings
        {
            get { return AggregationOptions.Select(x => x.Name).ToArray(); }
        }

        public static TimeSpanOption[] TimeSpanOptions = new[]
                                                             {
                                                                 new TimeSpanOption { Option = "1 Minute", TimeSpan = TimeSpan.FromMinutes(1) },
                                                                 new TimeSpanOption { Option = "5 Minutes", TimeSpan = TimeSpan.FromMinutes(5) },
                                                                 new TimeSpanOption { Option = "10 Minutes", TimeSpan = TimeSpan.FromMinutes(10) },
                                                                 new TimeSpanOption { Option = "15 Minutes", TimeSpan = TimeSpan.FromMinutes(15) },
                                                                 new TimeSpanOption { Option = "30 Minutes", TimeSpan = TimeSpan.FromMinutes(30) },
                                                                 new TimeSpanOption { Option = "Hourly", TimeSpan = TimeSpan.FromHours(1) },
                                                                 new TimeSpanOption { Option = "6 Hourly", TimeSpan = TimeSpan.FromHours(6) },
                                                                 new TimeSpanOption { Option = "12 Hourly", TimeSpan = TimeSpan.FromHours(12) },
                                                                 new TimeSpanOption { Option = "Daily", TimeSpan = TimeSpan.FromDays(1) },
                                                                 new TimeSpanOption { Option = "7 Days", TimeSpan = TimeSpan.FromDays(7) },
                                                                 new TimeSpanOption { Option = "30 Days", TimeSpan = TimeSpan.FromDays(30) },
                                                                 new TimeSpanOption { Option = "365 Days", TimeSpan = TimeSpan.FromDays(365) }
                                                             };

        public static IAggregationMethod[] AggregationOptions
        {
            get
            {
                return new IAggregationMethod[] { new Average(), new Sum(), new NearestValue(), new WeightedAverage()  };
            }
        }
    }
}
