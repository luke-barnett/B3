using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class HeatMapViewModel : BaseViewModel
    {
        private RenderTargetBitmap _heatMap;
        private readonly List<Sensor> _sensorsToUse;
        private List<Sensor> _availableSensors;

        public HeatMapViewModel()
        {
            _sensorsToUse = new List<Sensor>();
            _availableSensors = new List<Sensor>();
            _heatMap = new RenderTargetBitmap(2000, 2000, 96, 96, PixelFormats.Pbgra32);
            ClearMap();
        }

        public RenderTargetBitmap HeatMap
        {
            get { return _heatMap; }
            set
            {
                _heatMap = value;
                NotifyOfPropertyChange(() => _heatMap);
            }
        }

        public List<Sensor> AvailableSensors
        {
            get { return _availableSensors; }
            set
            {
                _availableSensors = value;
                NotifyOfPropertyChange(() => AvailableSensors);
                _sensorsToUse.AddRange(AvailableSensors.Take(10));
                DrawHeatMap();
            }
        }

        #region Private Methods
        private void DrawHeatMap()
        {

            var width = _heatMap.Width;
            var height = _heatMap.Height;

            var depths = _sensorsToUse.Select(x => x.Depth).ToArray();

            var minDepth = depths.Min();
            var maxDepth = depths.Max();


            var radius = height / depths.Distinct().Count();
            Debug.Print("Radius: {0}", radius);
            //No longer need depths
            depths = null;

            var depthRange = maxDepth - minDepth;

            var heightMultiplier = height / depthRange;

            Debug.Print("Depth Min {0} Max {1} Range {2} Multiplier {3}", minDepth, maxDepth, depthRange, heightMultiplier);

            var timeStamps = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Key).ToArray();

            var minTimeStamp = timeStamps.Min();
            var maxTimeStamp = timeStamps.Max();

            //No longer need timestamps
            timeStamps = null;

            var timeRange = maxTimeStamp - minTimeStamp;

            var widthMultiplier = width / timeRange.TotalMinutes;

            Debug.Print("Time Min {0} Max {1} Range {2} Multiplier {3}", minTimeStamp, maxTimeStamp, timeRange, widthMultiplier);

            var values = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Value).ToArray();

            var minValue = values.Min();
            var maxValue = values.Max();

            values = null;

            var valuesRange = maxValue - minValue;

            var valuesMultiplier = 120 / valuesRange;

            Debug.Print("Values Min {0} Max {1} Range {2} Multiplier {3}", minValue, maxValue, valuesRange, valuesMultiplier);

            foreach (var sensor in _sensorsToUse)
            {
                foreach (var timeStamp in sensor.CurrentState.Values.OrderBy(x => x.Key).Where((x, index) => index % 5 == 0))
                {
                    var intensity = (byte)((timeStamp.Value - minValue) * valuesMultiplier);
                    var radialGradientBrush = new RadialGradientBrush();
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(intensity, 0, 0, 0), 0.0));
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
                    radialGradientBrush.Freeze();

                    var x = ((timeStamp.Key - minTimeStamp).TotalMinutes * widthMultiplier) - radius;
                    var y = ((sensor.Depth - minDepth) * heightMultiplier) - radius;

                    Debug.Print("Point Timestamp {0} Value {1} Depth {2} Intensity {3} x {4} y {5}", timeStamp.Key, timeStamp.Value, sensor.Depth, intensity, x, y);

                    var drawingVisual = new DrawingVisual();
                    using (var context = drawingVisual.RenderOpen())
                    {
                        context.DrawRectangle(radialGradientBrush, null, new Rect(x, y, radius * 2, radius * 2));
                    }
                    HeatMap.Render(drawingVisual);
                }
            }
        }

        private void ClearMap()
        {
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, HeatMap.PixelWidth, HeatMap.PixelHeight));
            }
            HeatMap.Render(dv);
        }
        #endregion
    }
}
