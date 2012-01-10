using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace IndiaTango.Models
{
    public static class DataGridHelper
    {
        public static DataTable GenerateDataTable(IEnumerable<Sensor> sensorsToGenerateFrom, DateTime startTime, DateTime endTime)
        {
            var sensors = sensorsToGenerateFrom.Distinct(new SensorNameEqualityComparer()).OrderBy(x => x.Name).ToArray();

            if (sensors.Length == 0)
                return new DataTable();

            var table = new DataTable();

            table.Columns.Add(new DataColumn("Timestamp", typeof(DateTime)));

            foreach (var sensor in sensors)
            {
                table.Columns.Add(new DataColumn(sensor.Name.Replace(".", ""), typeof(string)));
            }

            for (var j = startTime; j <= endTime; j = j.AddMinutes(sensors[0].Owner.DataInterval))
            {
                var row = table.NewRow();
                row[0] = j;
                for (var i = 0; i < sensors.Length; i++)
                {
                    row[i + 1] = "";
                    if (sensors[i].CurrentState.Values.ContainsKey(j))
                        row[i + 1] = sensors[i].CurrentState.Values[j].ToString(CultureInfo.InvariantCulture);

                    if (sensors[i].RawData.Values.ContainsKey(j))
                        row[i + 1] += string.Format(" [{0}]",
                                                    sensors[i].RawData.Values[j]);
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

            #region Start Date

            var startDateRow = table.NewRow();

            startDateRow[0] = "Start Date";

            for (var i = 0; i < sensors.Length; i++)
            {
                var startDate = sensors[i].CurrentState.Values.OrderBy(x => x.Key).FirstOrDefault().Key;
                startDateRow[i + 1] = startDate != DateTime.MinValue
                                          ? startDate.ToString(CultureInfo.InvariantCulture)
                                          : "No Data";
            }

            table.Rows.Add(startDateRow);

            #endregion

            #region End Date

            var endDateRow = table.NewRow();

            endDateRow[0] = "End Date";

            for (var i = 0; i < sensors.Length; i++)
            {
                var startDate = sensors[i].CurrentState.Values.OrderBy(x => x.Key).LastOrDefault().Key;
                endDateRow[i + 1] = startDate != DateTime.MinValue
                                          ? startDate.ToString(CultureInfo.InvariantCulture)
                                          : "No Data";
            }

            table.Rows.Add(endDateRow);

            #endregion

            #region # Of Measurements

            var numOfMeasurements = table.NewRow();

            numOfMeasurements[0] = "# of measurements";

            for (var i = 0; i < sensors.Length; i++)
            {
                numOfMeasurements[i + 1] = sensors[i].CurrentState.Values.Count;
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

            #region Mean

            var mean = table.NewRow();

            mean[0] = "Mean";

            for (var i = 0; i < sensors.Length; i++)
            {
                mean[i + 1] = sensors[i].CurrentState.Values.Average(x => x.Value).ToString(CultureInfo.InvariantCulture);
            }

            table.Rows.Add(mean);

            #endregion

            #region Median

            var median = table.NewRow();

            median[0] = "Median";

            for (var i = 0; i < sensors.Length; i++)
            {
                median[i + 1] =
                    sensors[i].CurrentState.Values.Select(x => x.Value).Median().ToString(CultureInfo.InvariantCulture);
            }

            table.Rows.Add(median);

            #endregion

            #region Standard Dev

            var standardDev = table.NewRow();

            standardDev[0] = "Standard Deviation";

            for (var i = 0; i < sensors.Length; i++)
            {
                standardDev[i + 1] =
                    sensors[i].CurrentState.Values.Select(x => x.Value).StandardDeviation().ToString(CultureInfo.InvariantCulture);
            }

            table.Rows.Add(standardDev);

            #endregion

            return table;
        }
    }
}
