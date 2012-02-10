using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace IndiaTango.Models
{
    /// <summary>
    /// Helper for creating datagrids
    /// </summary>
    public static class DataGridHelper
    {
        public static DataTable GenerateDataTable(IEnumerable<Sensor> sensorsToGenerateFrom, DateTime startTime, DateTime endTime)
        {
            var sensors = sensorsToGenerateFrom.Distinct(new SensorNameEqualityComparer()).OrderBy(x => x.SortIndex).ToArray();

            if (sensors.Length == 0)
                return new DataTable();

            if (startTime < sensors[0].Owner.StartTimeStamp)
                startTime = sensors[0].Owner.StartTimeStamp;

            if (endTime > sensors[0].Owner.EndTimeStamp)
                endTime = sensors[0].Owner.EndTimeStamp;

            var table = new DataTable();

            table.Columns.Add(new DataColumn("Timestamp", typeof(FormattedDateTime)));

            foreach (var sensor in sensors)
            {
                table.Columns.Add(new DataColumn(sensor.Name.Replace(".", ""), typeof(string)));
            }

            for (var j = startTime.Round(new TimeSpan(0, 0, sensors[0].Owner.DataInterval, 0)); j <= endTime; j = j.AddMinutes(sensors[0].Owner.DataInterval))
            {
                var row = table.NewRow();
                row[0] = new FormattedDateTime(j);
                for (var i = 0; i < sensors.Length; i++)
                {
                    row[i + 1] = "";

                    var raw = string.Empty;
                    if (sensors[i].RawData.Values.ContainsKey(j))
                        raw = string.Format("{0}", sensors[i].RawData.Values[j]);

                    var current = string.Empty;
                    if (sensors[i].CurrentState.Values.ContainsKey(j))
                        current = string.Format("{0}", sensors[i].CurrentState.Values[j]);

                    if (String.CompareOrdinal(raw, current) == 0)
                        row[i + 1] = current;
                    else
                        row[i + 1] = current == string.Empty ? string.Format("[{0}]", raw) : string.Format("[{0}] {1}", raw, current);
                }
                table.Rows.Add(row);
            }

            return table;
        }

        public static DataTable GenerateSummaryStatistics(IEnumerable<Sensor> sensorsToGenerateFrom, DateTime startTime, DateTime endTime)
        {
            var sensors = sensorsToGenerateFrom.Distinct(new SensorNameEqualityComparer()).OrderBy(x => x.Name).ToArray();

            if (sensors.Length == 0)
                return new DataTable();

            var table = new DataTable();

            table.Columns.Add(new DataColumn("Statistic", typeof(string)));

            foreach (var sensor in sensors)
            {
                table.Columns.Add(new DataColumn(sensor.Name.Replace(".", ""), typeof(string)));
            }

            

            #region Start and End Dates

            var startDateRow = table.NewRow();

            startDateRow[0] = "Start Date";

            var endDateRow = table.NewRow();

            endDateRow[0] = "End Date";

            for (var i = 0; i < sensors.Length; i++)
            {
                var orderedTimeStamps =
                sensors[i].CurrentState.Values.Select(x => x.Key).Where(x => x >= startTime && x <= endTime).OrderBy(
                    x => x).ToArray();


                var startDate = orderedTimeStamps.FirstOrDefault();
                startDateRow[i + 1] = startDate != DateTime.MinValue
                                          ? startDate.ToString(CultureInfo.InvariantCulture)
                                          : "No Data";

                var endDate = orderedTimeStamps.LastOrDefault();
                endDateRow[i + 1] = endDate != DateTime.MinValue
                                          ? endDate.ToString(CultureInfo.InvariantCulture)
                                          : "No Data";
            }

            table.Rows.Add(startDateRow);
            table.Rows.Add(endDateRow);

            #endregion

            #region # Of Measurements

            var numOfMeasurements = table.NewRow();

            numOfMeasurements[0] = "# of measurements";

            for (var i = 0; i < sensors.Length; i++)
            {
                numOfMeasurements[i + 1] = sensors[i].CurrentState.Values.Count(x => x.Key >= startTime && x.Key <= endTime);
            }

            table.Rows.Add(numOfMeasurements);

            #endregion

            #region # Of Missing

            var numOfMissing = table.NewRow();

            numOfMissing[0] = "# of missing";

            for (var i = 0; i < sensors.Length; i++)
            {

                var count = 0;
                for (var j = startTime; j <= endTime; j = j.AddMinutes(sensors[0].Owner.DataInterval))
                {
                    if (!sensors[i].CurrentState.Values.ContainsKey(j))
                        count++;
                }
                numOfMissing[i + 1] = count;
            }

            table.Rows.Add(numOfMissing);

            #endregion

            var mean = table.NewRow();
            mean[0] = "Mean";

            var median = table.NewRow();
            median[0] = "Median";

            var standardDev = table.NewRow();
            standardDev[0] = "Standard Deviation";

            for (var i = 0; i < sensors.Length; i++)
            {
                var validValues = sensors[i].CurrentState.Values.Where(x => x.Key >= startTime && x.Key <= endTime).ToList();
                if (validValues.Count == 0) continue;

                mean[i + 1] = validValues.Average(x => x.Value).ToString(CultureInfo.InvariantCulture);
                median[i + 1] = validValues.Select(x => x.Value).Median().ToString(CultureInfo.InvariantCulture);
                standardDev[i + 1] = validValues.Select(x => x.Value).StandardDeviation().ToString(CultureInfo.InvariantCulture);
            }

            table.Rows.Add(mean);
            table.Rows.Add(median);
            table.Rows.Add(standardDev);

            #region Maximum

            var maximum = table.NewRow();

            maximum[0] = "Maximum";

            for (var i = 0; i < sensors.Length; i++)
            {
                maximum[i + 1] = sensors[i].CurrentState.Values.Select(x => x.Value).Max();
            }

            table.Rows.Add(maximum);

            #endregion

            #region Minimum

            var minimum = table.NewRow();

            minimum[0] = "Minimum";

            for (var i = 0; i < sensors.Length; i++)
            {
                minimum[i + 1] = sensors[i].CurrentState.Values.Select(x => x.Value).Min();
            }

            table.Rows.Add(minimum);

            #endregion

            return table;
        }
    }
}
