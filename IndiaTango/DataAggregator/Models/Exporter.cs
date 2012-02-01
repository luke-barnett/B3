﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace DataAggregator.Models
{
    public static class Exporter
    {
        public static bool Export(DateTime[] timestamps, Series[] series, TimeSpanOption aggregationPeriod, string filename, BackgroundWorker worker = null)
        {
            var progress = 0;
            var firstTimeStamp = timestamps.Min().RoundDown(aggregationPeriod.TimeSpan);
            var finalTimeStamp = timestamps.Max().RoundUp(aggregationPeriod.TimeSpan);
            var totalTimeSpan = finalTimeStamp - firstTimeStamp;

            var halfOfTimeSpan = aggregationPeriod.TimeSpan.Subtract(new TimeSpan(aggregationPeriod.TimeSpan.Ticks / 2));

            var aggregatedTimestamps = new List<DateTime>();

            for (var timestamp = firstTimeStamp; timestamp <= finalTimeStamp; timestamp = timestamp.Add(aggregationPeriod.TimeSpan))
            {
                aggregatedTimestamps.Add(timestamp);
                var inclusiveStart = timestamp - halfOfTimeSpan;
                var exclusiveEnd = timestamp + halfOfTimeSpan;

                var includedTimestamps = timestamps.Where(y => y >= inclusiveStart && y < exclusiveEnd).ToArray();

                foreach (var x in series)
                {
                    x.AggregatedValues[timestamp] =
                        x.AggregationModel.AggregationMethod.Aggregate(x.Values.Where(y => includedTimestamps.Contains(y.Key)).OrderBy(y => y.Key), inclusiveStart, exclusiveEnd);
                }

                if (worker == null || !worker.WorkerReportsProgress) continue;

                var currentTimeSpan = timestamp - firstTimeStamp;
                var currentProgress = (int)(currentTimeSpan.TotalSeconds / totalTimeSpan.TotalSeconds * 100);

                if (currentProgress <= progress) continue;

                worker.ReportProgress(currentProgress);
                progress = currentProgress;
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

        public static TimeSpan RoundUp(this TimeSpan time, TimeSpan roundingInterval)
        {
            return new TimeSpan(
                Convert.ToInt64(Math.Ceiling(
                    time.Ticks / (decimal)roundingInterval.Ticks
                )) * roundingInterval.Ticks
            );
        }

        public static DateTime RoundUp(this DateTime datetime, TimeSpan roundingInterval)
        {
            return new DateTime((datetime - DateTime.MinValue).RoundDown(roundingInterval).Ticks);
        }
    }
}