using System;
using System.Collections.Generic;
using System.Linq;

namespace IndiaTango.Models
{
    public static class LakeAnalysis
    {
        public static IEnumerable<DensitySeries> CalculateDensity(Dataset dataset)
        {
            var densitySeries = new List<DensitySeries>();

            foreach (var sensor in dataset.Sensors.Where(x => x.SensorType == "Water_Temperature"))
            {
                var series = new DensitySeries(sensor.Depth);
                foreach (var value in sensor.CurrentState.Values)
                {
                    var density = (1 -
                                   (((value.Value + 288.9414) / (508929.2 * (value.Value + 68.12963))) *
                                    Math.Pow((value.Value - 3.9863), 2))) * 1000;
                    series.AddValue(value.Key, density);
                }
                densitySeries.Add(series);
            }

            return densitySeries.ToArray();
        }

        public static Dictionary<DateTime, ThermoclineDepthDetails> CalculateThermoclineDepth(Dataset dataset, double mixedTempDifferential = 0, IEnumerable<DensitySeries> preCalculatedDensities = null, bool seasonal = false, float minimumMetalimionSlope = 0.1f)
        {
            var thermocline = new Dictionary<DateTime, ThermoclineDepthDetails>();
            var densities = (preCalculatedDensities == null) ? CalculateDensity(dataset).OrderBy(x => x.Depth).ToArray() : preCalculatedDensities.OrderBy(x => x.Depth).ToArray();

            var densityColumns = GenerateDensityColumns(densities);

            var timeStamps = densityColumns.Keys.ToArray();


            foreach (var t in timeStamps)
            {
                var thermoclineDetails = new ThermoclineDepthDetails();
                var depths = densityColumns[t].Keys.OrderBy(x => x).ToArray();
                if (depths.Length < 3) //We need at least 3 depths to calculate
                    continue;

                if (mixedTempDifferential > 0)
                {
                    var orderedSensors = dataset.Sensors.Where(x => x.SensorType == "Water_Temperature").OrderBy(x => x.Depth).ToArray();
                    if (orderedSensors.Any())
                    {
                        var first = orderedSensors.First(x => x.CurrentState.Values.ContainsKey(t));
                        var last = orderedSensors.Last(x => x.CurrentState.Values.ContainsKey(t));

                        if (first != null && last != null)
                        {
                            if (first.CurrentState.Values[t] - last.CurrentState.Values[t] <= mixedTempDifferential)
                                continue;
                        }
                    }
                }

                var slopes = new double[depths.Length];

                for (var i = 1; i < depths.Length - 1; i++)
                {
                    slopes[i] = (densityColumns[t][depths[i + 1]] - densityColumns[t][depths[i]]) /
                                (depths[i + 1] - depths[i]);
                }

                var maxSlope = slopes.Max();
                var indexOfMaxium = Array.IndexOf(slopes, maxSlope);

                thermoclineDetails.ThermoclineIndex = indexOfMaxium;
                thermoclineDetails.ThermoclineDepth = (depths[indexOfMaxium] + depths[indexOfMaxium + 1]) / 2;

                if (indexOfMaxium > 1 && indexOfMaxium < depths.Length - 1)
                {
                    var sdn = -(depths[indexOfMaxium + 1] - depths[indexOfMaxium]) / (slopes[indexOfMaxium + 1] - slopes[indexOfMaxium]);
                    var sup = (depths[indexOfMaxium] - depths[indexOfMaxium - 1]) / (slopes[indexOfMaxium] - slopes[indexOfMaxium - 1]);
                    var upD = depths[indexOfMaxium];
                    var dnD = depths[indexOfMaxium + 1];

                    if (!(double.IsInfinity(sdn) || double.IsInfinity(sup) || double.IsNaN(sdn) || double.IsNaN(sup)))
                    {
                        thermoclineDetails.ThermoclineDepth = (float)(dnD * (sdn / (sdn + sup)) + upD * (sup / (sdn + sup)));
                    }
                }

                if (seasonal)
                {
                    const float minPercentageForUniqueTheroclineStep = 0.15f;

                    var minCutPoint = Math.Max(minPercentageForUniqueTheroclineStep * maxSlope, minimumMetalimionSlope);

                    var localPeaks = LocalPeaks(slopes, minCutPoint);

                    if (localPeaks.Any())
                    {
                        var indexOfSeasonallyAdjustedMaximum = Array.IndexOf(slopes, localPeaks.Last());

                        if (indexOfSeasonallyAdjustedMaximum > indexOfMaxium + 1)
                        {
                            thermoclineDetails.SeasonallyAdjustedThermoclineIndex = indexOfSeasonallyAdjustedMaximum;
                            thermoclineDetails.SeasonallyAdjustedThermoclineDepth = (depths[indexOfSeasonallyAdjustedMaximum] + depths[indexOfSeasonallyAdjustedMaximum + 1]) / 2;

                            if (indexOfSeasonallyAdjustedMaximum > 1 && indexOfSeasonallyAdjustedMaximum < depths.Length - 1)
                            {
                                var sdn = -(depths[indexOfSeasonallyAdjustedMaximum + 1] - depths[indexOfSeasonallyAdjustedMaximum]) / (slopes[indexOfSeasonallyAdjustedMaximum + 1] - slopes[indexOfSeasonallyAdjustedMaximum]);
                                var sup = (depths[indexOfSeasonallyAdjustedMaximum] - depths[indexOfSeasonallyAdjustedMaximum - 1]) / (slopes[indexOfSeasonallyAdjustedMaximum] - slopes[indexOfSeasonallyAdjustedMaximum - 1]);
                                var upD = depths[indexOfSeasonallyAdjustedMaximum];
                                var dnD = depths[indexOfSeasonallyAdjustedMaximum + 1];

                                if (!(double.IsInfinity(sdn) || double.IsInfinity(sup) || double.IsNaN(sdn) || double.IsNaN(sup)))
                                {
                                    thermoclineDetails.SeasonallyAdjustedThermoclineDepth = (float)(dnD * (sdn / (sdn + sup)) + upD * (sup / (sdn + sup)));
                                }
                            }
                        }
                        else
                        {
                            thermoclineDetails.NoSeasonalFound();
                        }
                    }
                    else
                    {
                        thermoclineDetails.NoSeasonalFound();
                    }
                }

                thermocline[t] = thermoclineDetails;
            }

            return thermocline;
        }

        #region Helpers

        private static double[] LocalPeaks(double[] data, double dataMinimum)
        {
            var peaks = new List<double>();

            for (var i = 1; i < data.Length - 1; i++)
            {
                if (data[i - 1] < data[i] && data[i + 1] < data[i])
                    peaks.Add(data[i]);
            }

            return peaks.Where(x => x > dataMinimum).ToArray();
        }

        private static Dictionary<DateTime, Dictionary<float, double>> GenerateDensityColumns(DensitySeries[] densities)
        {
            var densityColumns = new Dictionary<DateTime, Dictionary<float, double>>();

            var timestamps = densities.SelectMany(x => x.Density.Keys).Distinct().OrderBy(x => x).ToArray();

            foreach (var timestamp in timestamps)
            {
                var column = new Dictionary<float, double>();
                var t = timestamp;
                foreach (var series in densities.Where(x => x.Density.ContainsKey(t)))
                {
                    column[series.Depth] = series.Density[timestamp];
                }
                densityColumns[timestamp] = column;
            }

            return densityColumns;
        }

        #endregion
    }
}
