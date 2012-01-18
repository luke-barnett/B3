using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IndiaTango.Effects;
using IndiaTango.Models;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace IndiaTango.ViewModels
{
    public class HeatMapViewModel : BaseViewModel
    {
        private readonly RenderTargetBitmap _heatMap;
        private RenderTargetBitmap _heatMapGraph;
        private readonly List<Sensor> _sensorsToUse;
        private List<Sensor> _availableSensors;
        private readonly HeatColorizer _heatMapColourEffect;
        private bool _specifyRadiusEnabled;
        private float _radius;

        public HeatMapViewModel()
        {
            _sensorsToUse = new List<Sensor>();
            _availableSensors = new List<Sensor>();
            _heatMap = new RenderTargetBitmap(2750, 2750, 96, 96, PixelFormats.Pbgra32);
            _heatMapGraph = new RenderTargetBitmap(3000, 3000, 96, 96, PixelFormats.Pbgra32);

            _heatMapColourEffect = new HeatColorizer
                                       {
                                           Palette = new VisualBrush
                                                         {
                                                             Visual = new Rectangle
                                                                          {
                                                                              Height = 1,
                                                                              Width = 256,
                                                                              Fill = GenerateHeatGradient()
                                                                          }
                                                         }
                                       };
        }

        public RenderTargetBitmap HeatMapGraph
        {
            get { return _heatMapGraph; }
            set
            {
                _heatMapGraph = value;
                NotifyOfPropertyChange(() => _heatMapGraph);
            }
        }

        public List<Sensor> AvailableSensors
        {
            get { return _availableSensors; }
            set
            {
                _availableSensors = value;
                NotifyOfPropertyChange(() => AvailableSensors);
            }
        }

        public List<Sensor> SelectedSensors { get; set; }

        public bool SpecifyRadiusEnabled
        {
            get { return _specifyRadiusEnabled; }
            set
            {
                _specifyRadiusEnabled = value;
                NotifyOfPropertyChange(() => SpecifyRadiusEnabled);
            }
        }

        public float Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                NotifyOfPropertyChange(() => Radius);
            }
        }

        #region Public Methods

        public void DrawGraph()
        {
            ClearRenderTargetBitmap(_heatMapGraph);


            const double xAxisOffset = 100;
            const double yAxisOffset = 250;
            const double topOffset = 100;

            var axisLength = _heatMapGraph.PixelWidth - 250;
            const double axisThickness = 10;
            var axisPen = new Pen(Brushes.Black, axisThickness);

            DrawHeatMap();
            var rect = new Rectangle
                           {
                               Fill = new ImageBrush(_heatMap),
                               Effect = _heatMapColourEffect
                           };

            var rectSize = new Size(_heatMap.PixelWidth, _heatMap.PixelHeight);

            rect.Measure(rectSize);
            rect.Arrange(new Rect(new Point(xAxisOffset + 5, topOffset), rectSize));

            _heatMapGraph.Render(rect);

            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                context.DrawLine(axisPen, new Point(xAxisOffset, _heatMapGraph.PixelHeight - yAxisOffset - axisLength + topOffset), new Point(xAxisOffset, _heatMapGraph.PixelHeight - yAxisOffset + topOffset));
                context.DrawLine(axisPen, new Point(xAxisOffset - 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset), new Point(xAxisOffset + axisLength + 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset));
            }

            _heatMapGraph.Render(drawingVisual);
        }

        public void AddToSelected(ListBox listBox)
        {
            if (listBox.SelectedItem != null && listBox.SelectedItem is Sensor)
            {
                var sensor = (Sensor)listBox.SelectedItem;
                if (!_sensorsToUse.Contains(sensor))
                    _sensorsToUse.Add(sensor);
            }
            SelectedSensors = new List<Sensor>(_sensorsToUse);
            NotifyOfPropertyChange(() => SelectedSensors);
        }

        public void RemoveFromSelected(ListBox listBox)
        {
            if (listBox.SelectedItem != null && listBox.SelectedItem is Sensor)
            {
                var sensor = (Sensor)listBox.SelectedItem;
                _sensorsToUse.Remove(sensor);
            }
            SelectedSensors = new List<Sensor>(_sensorsToUse);
            NotifyOfPropertyChange(() => SelectedSensors);
        }

        #endregion

        #region Private Methods

        private void DrawHeatMap()
        {
            ClearRenderTargetBitmap(_heatMap);
            if (_sensorsToUse.Count == 0)
            {
                Common.ShowMessageBox("Can't render", "You haven't selected any sensors to render the heatmap from",
                                      false, true);
                return;
            }

            var width = _heatMap.Width;
            var height = _heatMap.Height;

            var depths = _sensorsToUse.Select(x => x.Depth).ToArray();

            var minDepth = depths.Min();
            var maxDepth = depths.Max();


            if (!SpecifyRadiusEnabled)
                Radius = (float)height / depths.Distinct().Count();
            Debug.Print("Radius: {0}", Radius);
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

                    var x = ((timeStamp.Key - minTimeStamp).TotalMinutes * widthMultiplier) - Radius;
                    var y = ((sensor.Depth - minDepth) * heightMultiplier) - Radius;

                    Debug.Print("Point Timestamp {0} Value {1} Depth {2} Intensity {3} x {4} y {5}", timeStamp.Key, timeStamp.Value, sensor.Depth, intensity, x, y);

                    var drawingVisual = new DrawingVisual();
                    using (var context = drawingVisual.RenderOpen())
                    {
                        context.DrawRectangle(radialGradientBrush, null, new Rect(x, y, Radius * 2, Radius * 2));
                    }
                    _heatMap.Render(drawingVisual);
                }
            }
        }

        private static void ClearRenderTargetBitmap(RenderTargetBitmap renderTargetBitmap)
        {
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight));
            }
            renderTargetBitmap.Render(dv);
        }

        private static LinearGradientBrush GenerateHeatGradient()
        {
            var brush = new LinearGradientBrush();
            for (double i = 0; i < 1; i += 0.01)
            {
                brush.GradientStops.Add(new GradientStop(HSL2RGB(i, 0.5, 0.5), i));
                Debug.Print("Colour[{0}] {1}", i, HSL2RGB(i, 0.5, 0.5));
            }
            brush.Freeze();
            return brush;
        }

        private static Color HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;

            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            return Color.FromRgb(Convert.ToByte(r * 255.0f), Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f));
        }

        #endregion
    }
}
