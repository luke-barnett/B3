using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataAggregator.Models
{
    public static class Exporter
    {
        public static bool Export(DateTime[] timestamps, Series[] series, TimeSpanOption aggregationPeriod, string filename)
        {
            var firstTimeStamp = timestamps.Min().RoundDown(aggregationPeriod.TimeSpan);

            var aggregatedTimestamps = new List<DateTime>();
            var maxTimestamp = timestamps.Max();
            for (var timestamp = firstTimeStamp; timestamp < maxTimestamp; timestamp = timestamp.Add(aggregationPeriod.TimeSpan))
            {
                var inclusiveStart = timestamp;
                var exclusiveEnd = timestamp.Add(aggregationPeriod.TimeSpan);

                aggregatedTimestamps.Add(inclusiveStart);

                foreach (var x in series)
                {
                    var includedTimestamps = timestamps.Where(y => y >= inclusiveStart && y < exclusiveEnd).ToArray();
                    x.AggregatedValues[inclusiveStart] =
                        x.AggregationModel.AggregationMethod.Aggregate(x.Values.Where(y => includedTimestamps.Contains(y.Key)).OrderBy(y => y.Key).Select(y => y.Value), inclusiveStart, exclusiveEnd);
                }
            }
            try
            {
                using (var writer = File.CreateText(filename))
                {
                    writer.Write("YYYY/MM/DD hh:mm");
                    foreach (var x in series)
                    {
                        writer.Write("," + x.Name);
                    }

                    foreach (var aggregatedTimestamp in aggregatedTimestamps)
                    {
                        writer.Write(writer.NewLine);
                        writer.Write(aggregatedTimestamp.ToString("yyyy/MM/dd HH:mm"));
                        foreach (var x in series)
                        {
                            writer.Write(",");
                            if (x.AggregatedValues.ContainsKey(aggregatedTimestamp) && !float.IsNaN(x.AggregatedValues[aggregatedTimestamp]))
                            {
                                writer.Write(x.AggregatedValues[aggregatedTimestamp]);
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static TimeSpan RoundDown(this TimeSpan time, TimeSpan roundingInterval)
        {
            return new TimeSpan(
                Convert.ToInt64(Math.Floor(
                    time.Ticks / (decimal)roundingInterval.Ticks
                )) * roundingInterval.Ticks
            );
        }

        public static DateTime RoundDown(this DateTime datetime, TimeSpan roundingInterval)
        {
            return new DateTime((datetime - DateTime.MinValue).RoundDown(roundingInterval).Ticks);
        }
    }
}
