using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private float _minDepth;
        private float _maxDepth;
        private DateTime _minTimestamp;
        private DateTime _maxTimestamp;
        private float _minValue;
        private float _maxValue;
        private List<DepthYValue> _depths;

        public HeatMapViewModel()
        {
            _sensorsToUse = new List<Sensor>();
            _availableSensors = new List<Sensor>();
            _heatMap = new RenderTargetBitmap(5000, 5000, 96, 96, PixelFormats.Pbgra32);
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

        public float MinDepth
        {
            get { return _minDepth; }
            set
            {
                _minDepth = value;
                NotifyOfPropertyChange(() => MinDepth);
            }
        }

        public float MaxDepth
        {
            get { return _maxDepth; }
            set
            {
                _maxDepth = value;
                NotifyOfPropertyChange(() => MaxDepth);
            }
        }

        public DateTime MinTimestamp
        {
            get { return _minTimestamp; }
            set
            {
                _minTimestamp = value;
                NotifyOfPropertyChange(() => MinTimestamp);
            }
        }

        public DateTime MaxTimestamp
        {
            get { return _maxTimestamp; }
            set
            {
                _maxTimestamp = value;
                NotifyOfPropertyChange(() => MaxTimestamp);
            }
        }

        public float MinValue
        {
            get { return _minValue; }
            set
            {
                _minValue = value;
                NotifyOfPropertyChange(() => MinValue);
            }
        }

        public float MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = value;
                NotifyOfPropertyChange(() => MaxValue);
            }
        }

        #region Public Methods

        public void DrawGraph()
        {
            ClearRenderTargetBitmap(_heatMapGraph);


            const double xAxisOffset = 250;
            const double yAxisOffset = 500;
            const double topOffset = 100;

            var axisLength = _heatMapGraph.PixelWidth - (int)yAxisOffset;
            const double axisThickness = 10;
            var axisPen = new Pen(Brushes.Black, axisThickness);

            DrawHeatMap();
            var heatMapRect = new Rectangle
                           {
                               Fill = new ImageBrush(_heatMap) { Stretch = Stretch.Uniform },
                               Effect = _heatMapColourEffect
                           };

            var heatMapRectSize = new Size(axisLength, axisLength);

            heatMapRect.Measure(heatMapRectSize);
            heatMapRect.Arrange(new Rect(new Point(xAxisOffset + 5, topOffset), heatMapRectSize));

            _heatMapGraph.Render(heatMapRect);

            var heatMapKeyWidth = axisLength;
            const int heatMapKeyHeight = 100;

            var heatMapKeyRect = new Rectangle
                                     {
                                         Fill = new ImageBrush(DrawHeatKey(heatMapKeyWidth, heatMapKeyHeight)),
                                         Effect = _heatMapColourEffect
                                     };

            var heatMapKeySize = new Size(heatMapKeyWidth, heatMapKeyHeight);

            heatMapKeyRect.Measure(heatMapKeySize);
            heatMapKeyRect.Arrange(new Rect(new Point(xAxisOffset + 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset + 150), heatMapKeySize));

            _heatMapGraph.Render(heatMapKeyRect);

            var drawingVisual = new DrawingVisual();
            var textTypeFace = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            const double headerFontSize = 80d;
            const double fontSize = 40d;
            const int numberOfTicks = 10;
            var fontBrush = Brushes.Black;
            using (var context = drawingVisual.RenderOpen())
            {
                context.DrawLine(axisPen, new Point(xAxisOffset, _heatMapGraph.PixelHeight - yAxisOffset - axisLength + topOffset), new Point(xAxisOffset, _heatMapGraph.PixelHeight - yAxisOffset + topOffset));
                context.DrawLine(axisPen, new Point(xAxisOffset - 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset), new Point(xAxisOffset + axisLength + 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset));

                context.DrawText(new FormattedText(MinValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point(xAxisOffset + 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset + 250));
                context.DrawText(new FormattedText(MaxValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point(xAxisOffset + 5 + axisLength, _heatMapGraph.PixelHeight - yAxisOffset + topOffset + 250));

                context.DrawText(new FormattedText("Time", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point(xAxisOffset + (axisLength / 2), topOffset + axisLength + 10));

                context.DrawText(new FormattedText(MinTimestamp.ToString("yyyy/MM/dd HH:mm"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, fontSize, fontBrush), new Point(xAxisOffset + 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset));
                context.DrawText(new FormattedText(MaxTimestamp.ToString("yyyy/MM/dd HH:mm"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, fontSize, fontBrush), new Point(xAxisOffset + 5 + axisLength, _heatMapGraph.PixelHeight - yAxisOffset + topOffset));

                context.PushTransform(new RotateTransform(90));

                context.DrawText(new FormattedText("Depth", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point((topOffset + (axisLength / 2)), -headerFontSize));

                context.Pop();

                foreach (var depthYValue in _depths)
                {
                    context.DrawText(new FormattedText(depthYValue.Depth.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, fontSize, fontBrush), new Point(xAxisOffset - 100, topOffset + (depthYValue.YValue / _heatMap.PixelHeight) * axisLength));
                }
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

            var depths = _sensorsToUse.Select(x => x.Depth).Distinct().OrderBy(x => x).ToArray();

            MinDepth = depths.Min();
            MaxDepth = depths.Max();


            if (!SpecifyRadiusEnabled)
                Radius = (float)height / (depths.Count() - 1);
            Debug.Print("Radius: {0}", Radius);

            var depthRange = MaxDepth - MinDepth;

            var heightMultiplier = height / (depths.Count() - 1);

            Debug.Print("Depth Min {0} Max {1} Range {2} Multiplier {3}", MinDepth, MaxDepth, depthRange, heightMultiplier);

            var timeStamps = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Key).ToArray();

            MinTimestamp = timeStamps.Min();
            MaxTimestamp = timeStamps.Max();

            //No longer need timestamps
            timeStamps = null;

            var timeRange = MaxTimestamp - MinTimestamp;

            var widthMultiplier = width / timeRange.TotalMinutes;

            Debug.Print("Time Min {0} Max {1} Range {2} Multiplier {3}", MinTimestamp, MaxTimestamp, timeRange, widthMultiplier);

            var values = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Value).ToArray();

            MinValue = values.Min();
            MaxValue = values.Max();

            values = null;

            var valuesRange = MaxValue - MinValue;

            var valuesMultiplier = 255 / valuesRange;

            Debug.Print("Values Min {0} Max {1} Range {2} Multiplier {3}", MinValue, MaxValue, valuesRange, valuesMultiplier);
            _depths = new List<DepthYValue>();
            foreach (var sensor in _sensorsToUse)
            {
                var y = (Array.IndexOf(depths, sensor.Depth) * heightMultiplier);
                _depths.Add(new DepthYValue(sensor.Depth, y));
                foreach (var timeStamp in sensor.CurrentState.Values.OrderBy(x => x.Key).Where((x, index) => index % 5 == 0))
                {
                    var intensity = (byte)((timeStamp.Value - MinValue) * valuesMultiplier);
                    var radialGradientBrush = new RadialGradientBrush();
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(intensity, 0, 0, 0), 0.0));
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
                    radialGradientBrush.Freeze();

                    var x = ((timeStamp.Key - MinTimestamp).TotalMinutes * widthMultiplier) - Radius;


                    Debug.Print("Point Timestamp {0} Value {1} Depth {2} Intensity {3} x {4} y {5}", timeStamp.Key, timeStamp.Value, sensor.Depth, intensity, x, y - Radius);

                    var drawingVisual = new DrawingVisual();
                    using (var context = drawingVisual.RenderOpen())
                    {
                        context.DrawRectangle(radialGradientBrush, null, new Rect(x, y, Radius * 2, Radius * 2));
                    }
                    _heatMap.Render(drawingVisual);
                }
            }
        }

        private RenderTargetBitmap DrawHeatKey(int width, int height)
        {
            var heatKeyBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            ClearRenderTargetBitmap(heatKeyBitmap);
            var brush = new LinearGradientBrush();
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
            brush.Freeze();
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(brush, null, new Rect(0, 0, heatKeyBitmap.PixelWidth, heatKeyBitmap.PixelHeight));
            }
            heatKeyBitmap.Render(visual);
            return heatKeyBitmap;
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
            /*for (double i = 0; i < 1; i += 0.01)
            {
                brush.GradientStops.Add(new GradientStop(HSL2RGB(i, 0.5, 0.5), i));
                Debug.Print("Colour[{0}] {1}", i, HSL2RGB(i, 0.5, 0.5));
            }*/
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.975d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.95d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.925d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.9d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.875d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.85d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.825d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.8d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.775d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.75d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.725d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.7d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.675d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.65d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.625d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.5d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.575d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.55d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.525d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.4d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.475d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.45d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.425d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.4d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.375d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.35d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.325d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.3d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.275d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.25d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 0.225d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 240), 0.2d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 200), 0.175d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 180), 0.15d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 170), 0.125d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 120), 0.1d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 100), 0.075d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 80), 0.05d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 60), 0.025d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 40), 0.0d));
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

    internal class DepthYValue
    {
        public readonly double Depth;
        public readonly double YValue;

        public DepthYValue(double depth, double yValue)
        {
            Depth = depth;
            YValue = yValue;
        }
    }
}
