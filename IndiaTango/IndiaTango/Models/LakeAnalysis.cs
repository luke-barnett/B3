using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace IndiaTango.Models
{
    /// <summary>
    /// Set of lake analysis calculations
    /// </summary>
    public static class LakeAnalysis
    {
        /// <summary>
        /// Calculates the densities for the dataset
        /// </summary>
        /// <param name="dataset">The dataset to calculate from</param>
        /// <returns>The list of densities</returns>
        public static IEnumerable<DensitySeries> CalculateDensity(Dataset dataset)
        {
            var densitySeries = new List<DensitySeries>();

            foreach (var sensor in dataset.Sensors.Where(x => x.SensorType == "Water_Temperature"))
            {
                var series = new DensitySeries(sensor.Elevation);
                foreach (var value in sensor.CurrentState.Values)
                {
                    var density = (1 - (((value.Value + 288.9414) / (508929.2 * (value.Value + 68.12963))) *
                                    Math.Pow((value.Value - 3.9863), 2))) * 1000;
                    series.AddValue(value.Key, density);
                }
                densitySeries.Add(series);
            }

            return densitySeries.ToArray();
        }

        /// <summary>
        /// Calculate the thermocline depth
        /// </summary>
        /// <param name="dataset">The dataset to use</param>
        /// <param name="mixedTempDifferential">The minimum mixed temp differnetial</param>
        /// <param name="preCalculatedDensities">The set of precalculated densities</param>
        /// <param name="seasonal">Whether or not to look check that values aren't seasonal</param>
        /// <param name="minimumMetalimionSlope">The minimum metalimion slope</param>
        /// <returns></returns>
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
                    var orderedSensors = dataset.Sensors.Where(x => x.SensorType == "Water_Temperature").OrderBy(x => x.Elevation).ToArray();
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

                thermoclineDetails.DrhoDz = slopes;

                var maxSlope = slopes.Max();
                var indexOfMaxium = Array.IndexOf(slopes, maxSlope);

                thermoclineDetails.ThermoclineIndex = indexOfMaxium;
                thermoclineDetails.ThermoclineDepth = (depths[indexOfMaxium] + depths[indexOfMaxium + 1]) / 2;

                if (indexOfMaxium > 1 && indexOfMaxium < depths.Length - 2)
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

                            if (indexOfSeasonallyAdjustedMaximum > 1 && indexOfSeasonallyAdjustedMaximum < depths.Length - 2)
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
                else
                {
                    thermoclineDetails.NoSeasonalFound();
                }

                thermocline[t] = thermoclineDetails;
            }

            return thermocline;
        }

        /// <summary>
        /// Calculates the metalimnion boundaries
        /// </summary>
        /// <param name="dataset">The dataset to use</param>
        /// <param name="thermoclineDepths">The precalculated thermocline depths for the dataset</param>
        /// <param name="minimumMetalimionSlope">The minimum metalimnion slope</param>
        /// <returns></returns>
        public static Dictionary<DateTime, MetalimnionBoundariesDetails> CalculateMetalimnionBoundaries(Dataset dataset, Dictionary<DateTime, ThermoclineDepthDetails> thermoclineDepths, float minimumMetalimionSlope = 0.1f)
        {
            var metalimnionBoundaries = new Dictionary<DateTime, MetalimnionBoundariesDetails>();

            foreach (var timestamp in thermoclineDepths.Keys)
            {
                var depths = dataset.Sensors.Where(x => x.SensorType == "Water_Temperature" && x.CurrentState.Values.ContainsKey(timestamp)).Select(x => x.Elevation).Distinct().OrderBy(x => x).ToArray();

                var meanDepths = new float[depths.Length - 1];

                for (var i = 0; i < depths.Length - 1; i++)
                {
                    meanDepths[i] = (depths[i] + depths[i + 1]) / 2;
                }

                var metalimnionBoundary = new MetalimnionBoundariesDetails();

                var sortedDepths = meanDepths.Union(new[] { thermoclineDepths[timestamp].ThermoclineDepth }).OrderBy(x => x).ToArray();
                var sortedDepthsParent = meanDepths.Union(new[] { thermoclineDepths[timestamp].SeasonallyAdjustedThermoclineDepth }).OrderBy(x => x).ToArray();

                var points = new Point[meanDepths.Length];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = new Point(meanDepths[i], thermoclineDepths[timestamp].DrhoDz[i]);
                }

                var slopes = Interpolate(points, sortedDepths).ToArray();
                var slopesParent = Interpolate(points, sortedDepthsParent).ToArray();

                var thermoclineIndex = Array.IndexOf(slopes.Select(x => x.X).ToArray(), thermoclineDepths[timestamp].ThermoclineDepth);
                var thermoclineIndexParent = Array.IndexOf(slopesParent.Select(x => x.X).ToArray(), thermoclineDepths[timestamp].SeasonallyAdjustedThermoclineDepth);

                #region Top

                metalimnionBoundary.Top = meanDepths[0];
                int k;
                for (k = thermoclineIndex; k > -1; k--)
                {
                    if (slopes[k].Y < minimumMetalimionSlope)
                    {
                        metalimnionBoundary.Top = sortedDepths[k];
                        break;
                    }
                }

                if (k == -1)
                    k = 0;

                if (thermoclineIndex - k > 1 && slopes[thermoclineIndex].Y > minimumMetalimionSlope)
                {
                    var outsidePoints = new List<Point>();
                    for (var j = k; j <= thermoclineIndex; j++)
                    {
                        outsidePoints.Add(new Point(slopes[j].Y, sortedDepths[j]));
                    }
                    metalimnionBoundary.Top = (float)Interpolate(outsidePoints.ToArray(), new[] { minimumMetalimionSlope })[0].Y;
                }

                #endregion

                #region Bottom

                metalimnionBoundary.Bottom = meanDepths.Last();

                for (k = thermoclineIndex; k < slopes.Length; k++)
                {
                    if (slopes[k].Y < minimumMetalimionSlope)
                    {
                        metalimnionBoundary.Bottom = sortedDepths[k];
                        break;
                    }
                }

                if (k == slopes.Length)
                    k--;

                if (k - thermoclineIndex > 1 && slopes[thermoclineIndex].Y > minimumMetalimionSlope)
                {
                    var outsidePoints = new List<Point>();
                    for (var j = thermoclineIndex; j <= k; j++)
                    {
                        outsidePoints.Add(new Point(slopes[j].Y, sortedDepths[j]));
                    }
                    metalimnionBoundary.Bottom = (float)Interpolate(outsidePoints.ToArray(), new[] { minimumMetalimionSlope })[0].Y;
                }

                #endregion

                #region IfParent

                if (thermoclineDepths[timestamp].HasSeaonallyAdjusted)
                {
                    #region Top

                    metalimnionBoundary.SeasonallyAdjustedTop = meanDepths[0];
                    int m;
                    for (m = thermoclineIndexParent; m > -1; m--)
                    {
                        if (slopesParent[m].Y < minimumMetalimionSlope)
                        {
                            metalimnionBoundary.SeasonallyAdjustedTop = sortedDepthsParent[m];
                            break;
                        }
                    }

                    if (m == -1)
                        m = 0;

                    if (thermoclineIndexParent - m > 0 && slopesParent[thermoclineIndexParent].Y > minimumMetalimionSlope)
                    {
                        var outsidePoints = new List<Point>();
                        for (var j = m; j <= thermoclineIndexParent; j++)
                        {
                            outsidePoints.Add(new Point(slopesParent[j].Y, sortedDepthsParent[j]));
                        }
                        metalimnionBoundary.SeasonallyAdjustedTop = (float)Interpolate(outsidePoints.ToArray(), new[] { minimumMetalimionSlope })[0].Y;
                    }

                    #endregion

                    #region Bottom

                    metalimnionBoundary.SeasonallyAdjustedBottom = meanDepths.Last();

                    for (m = thermoclineIndexParent; m < slopesParent.Length; m++)
                    {
                        if (slopesParent[m].Y < minimumMetalimionSlope)
                        {
                            metalimnionBoundary.SeasonallyAdjustedBottom = sortedDepthsParent[m];
                            break;
                        }
                    }

                    if (m == slopes.Length)
                        m--;

                    if (m - thermoclineIndexParent > 0 && slopesParent[thermoclineIndexParent].Y > minimumMetalimionSlope)
                    {
                        var outsidePoints = new List<Point>();
                        for (var j = thermoclineIndexParent; j <= m; j++)
                        {
                            outsidePoints.Add(new Point(slopesParent[j].Y, sortedDepthsParent[j]));
                        }
                        metalimnionBoundary.SeasonallyAdjustedBottom = (float)Interpolate(outsidePoints.ToArray(), new[] { minimumMetalimionSlope })[0].Y;
                    }

                    #endregion
                }
                else
                {
                    metalimnionBoundary.NoSeasonalFound();
                }

                #endregion

                metalimnionBoundaries[timestamp] = metalimnionBoundary;
            }

            return metalimnionBoundaries;
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

        /*public static Point[] Interpolate(Point[] points, float[] xi)
        {
            var interpolatedvalues = new List<Point>();

            points = points.OrderBy(x => x.X).ToArray();

            //Assumes inorder enumerables
            var i = 0;

            foreach (var xValue in xi)
            {
                while (points[i].X < xValue)
                {
                    i++;
                    if (i >= points.Length)
                        break;
                }

                if (i >= points.Length)
                    break;


                if (Math.Abs(points[i].X - xValue) < Double.Epsilon)
                {
                    interpolatedvalues.Add(points[i]);
                }
                else
                {
                    var x0 = 0d;
                    var y0 = 0d;

                    if (i > 0)
                    {
                        x0 = points[i - 1].X;
                        y0 = points[i - 1].Y;
                    }

                    var x1 = points[i].X;
                    var y1 = points[i].Y;

                    var yValue = y0 * (xValue - x1) / (x0 - x1) + y1 * (xValue - x0) / (x1 - x0);
                    interpolatedvalues.Add(new Point(xValue, yValue));
                }
            }


            return interpolatedvalues.OrderBy(x => x.X).ToArray();
        }*/

        public static Point[] Interpolate(Point[] points, float[] xi)
        {
            var interpolatedPoints = new Point[xi.Length];

            for (var i = 0; i < xi.Length; i++)
            {
                interpolatedPoints[i] = new Point(xi[i], Interp1(points.Select(x => x.X).ToList(), points.Select(x => x.Y).ToList(), xi[i]));
            }

            return interpolatedPoints;
        }

        /// <summary>
        /// Written for Virtual Photonics by Lisa Malenfant
        /// http://virtualphotonics.codeplex.com/SourceControl/changeset/view/d67a776726d5#src%2fVts%2fCommon%2fMath%2fInterpolation.cs
        /// </summary>
        public static double Interp1(IList<double> x, IList<double> y, double xi)
        {
            if (x.Count != y.Count)
            {
                throw new ArgumentException("Error in interp1: arrays x and y are not the same size!");
            }

            var currentIndex = 1;

            // changed this to clip to bounds (DC - 7/26/09)
            if ((xi < x[0]))
                return y[0];
            if ((xi > x[x.Count - 1]))
                return y[y.Count - 1];

            // increment the index until you pass the desired interpolation point
            while (x[currentIndex] < xi) currentIndex++;

            // then do the interp between x[currentIndex-1] and xi[currentIndex]
            var t = (xi - x[currentIndex - 1]) / (x[currentIndex] - x[currentIndex - 1]);
            return y[currentIndex - 1] + t * (y[currentIndex] - y[currentIndex - 1]);
        }

        #endregion
    }
}
