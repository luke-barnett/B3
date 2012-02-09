using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private bool _featuresEnabled = true;

        private bool _specifyRadiusEnabled;
        private bool _specifyMinTimestampEnabled;
        private bool _specifyMaxTimestampEnabled;
        private bool _specifyMinValueEnabled;
        private bool _specifyMaxValueEnabled;
        private bool _specifySamplingRate;

        private float _radius;
        private float _minDepth;
        private float _maxDepth;
        private DateTime _minTimestamp;
        private DateTime _maxTimestamp;
        private float _minValue;
        private float _maxValue;

        private List<DepthYValue> _depths;
        private int _samplingRate = 1;

        public HeatMapViewModel()
        {
            _sensorsToUse = new List<Sensor>();
            _availableSensors = new List<Sensor>();
            _heatMap = new RenderTargetBitmap(10000, 5000, 96, 96, PixelFormats.Pbgra32);
            _heatMapGraph = new RenderTargetBitmap(5000, 2800, 96, 96, PixelFormats.Pbgra32);

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

        public bool FeaturesEnabled
        {
            get { return _featuresEnabled; }
            set
            {
                _featuresEnabled = value;
                NotifyOfPropertyChange(() => FeaturesEnabled);
            }
        }

        #region Checkbox Values

        public bool SpecifyRadiusEnabled
        {
            get { return _specifyRadiusEnabled; }
            set
            {
                _specifyRadiusEnabled = value;
                NotifyOfPropertyChange(() => SpecifyRadiusEnabled);
            }
        }

        public bool SpecifyMinTimestampEnabled
        {
            get { return _specifyMinTimestampEnabled; }
            set
            {
                _specifyMinTimestampEnabled = value;
                NotifyOfPropertyChange(() => SpecifyMinTimestampEnabled);
            }
        }

        public bool SpecifyMaxTimestampEnabled
        {
            get { return _specifyMaxTimestampEnabled; }
            set
            {
                _specifyMaxTimestampEnabled = value;
                NotifyOfPropertyChange(() => SpecifyMaxTimestampEnabled);
            }
        }

        public bool SpecifyMinValueEnabled
        {
            get { return _specifyMinValueEnabled; }
            set
            {
                _specifyMinValueEnabled = value;
                NotifyOfPropertyChange(() => SpecifyMinValueEnabled);
            }
        }

        public bool SpecifyMaxValueEnabled
        {
            get { return _specifyMaxValueEnabled; }
            set
            {
                _specifyMaxValueEnabled = value;
                NotifyOfPropertyChange(() => SpecifyMaxValueEnabled);
            }
        }

        public bool SpecifySamplingRateEnabled
        {
            get { return _specifySamplingRate; }
            set
            {
                _specifySamplingRate = value;
                NotifyOfPropertyChange(() => SpecifySamplingRateEnabled);
            }
        }

        #endregion

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

        public int SamplingRate
        {
            get { return _samplingRate; }
            set
            {
                _samplingRate = value <= 0 ? 1 : value;
                NotifyOfPropertyChange(() => SamplingRate);
            }
        }

        #region Public Methods

        public void DrawGraph()
        {
            DisableFeatures();
            RenderGraph();
            EnableFeatures();
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

        private void RenderGraph()
        {
            ClearRenderTargetBitmap(_heatMapGraph, Brushes.White);


            const double xAxisOffset = 250;
            const double yAxisOffset = 500;
            const double topOffset = 50;
            const double rightOffset = 250;

            var xAxisLength = _heatMapGraph.PixelWidth - (int)xAxisOffset - (int)rightOffset;
            var yAxisLength = _heatMapGraph.PixelHeight - (int)yAxisOffset - (int)topOffset;
            const double axisThickness = 10;
            var axisPen = new Pen(Brushes.Black, axisThickness);
            var axisTickPen = new Pen(Brushes.Black, axisThickness / 2);

            DrawHeatMap();
            var heatMapRect = new Rectangle
            {
                Fill = new ImageBrush(_heatMap) { Stretch = Stretch.Uniform },
                Effect = _heatMapColourEffect
            };

            var heatMapRectSize = new Size(xAxisLength, yAxisLength);

            heatMapRect.Measure(heatMapRectSize);
            heatMapRect.Arrange(new Rect(new Point(xAxisOffset + 5, topOffset), heatMapRectSize));

            _heatMapGraph.Render(heatMapRect);

            var heatMapKeyWidth = xAxisLength;
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
                //Y Axis
                context.DrawLine(axisPen, new Point(xAxisOffset, topOffset), new Point(xAxisOffset, topOffset + yAxisLength));
                //X Axis
                context.DrawLine(axisPen, new Point(xAxisOffset - 5, topOffset + yAxisLength), new Point(xAxisOffset + xAxisLength + 5, topOffset + yAxisLength));

                context.DrawText(new FormattedText(MinValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point(xAxisOffset + 5, _heatMapGraph.PixelHeight - yAxisOffset + topOffset + 250));
                context.DrawText(new FormattedText(MaxValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point(xAxisOffset + 5 + xAxisLength, _heatMapGraph.PixelHeight - yAxisOffset + topOffset + 250));

                context.DrawText(new FormattedText("Time", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point(xAxisOffset + (xAxisLength / 2), topOffset + yAxisLength + 60));

                var dateDistance = (MaxTimestamp - MinTimestamp).Ticks / numberOfTicks;
                for (var i = 0; i < numberOfTicks; i++)
                {
                    var date = MinTimestamp + new TimeSpan(i * dateDistance);
                    var xValue = (1 - ((MaxTimestamp - date).TotalSeconds / (MaxTimestamp - MinTimestamp).TotalSeconds)) * xAxisLength + xAxisOffset;
                    context.DrawLine(axisTickPen, new Point(xValue, topOffset + yAxisLength), new Point(xValue, topOffset + yAxisLength + 20));
                    context.DrawText(new FormattedText(date.ToString("yyyy/MM/dd HH:mm"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, fontSize, fontBrush), new Point(xValue, topOffset + yAxisLength + 10));
                }

                context.PushTransform(new RotateTransform(90));

                context.DrawText(new FormattedText("Depth", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, headerFontSize, fontBrush), new Point((topOffset + (yAxisLength / 2)), -headerFontSize * 2));

                context.Pop();

                if (_depths != null)
                    foreach (var depthYValue in _depths)
                    {
                        context.DrawText(new FormattedText(depthYValue.Depth.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, textTypeFace, fontSize, fontBrush), new Point(xAxisOffset - 100, topOffset + (depthYValue.YValue / _heatMap.PixelHeight) * yAxisLength));
                        context.DrawLine(axisTickPen, new Point(xAxisOffset - 25, topOffset + (depthYValue.YValue / _heatMap.PixelHeight) * yAxisLength + (axisTickPen.Thickness / 2)), new Point(xAxisOffset, topOffset + (depthYValue.YValue / _heatMap.PixelHeight) * yAxisLength + (axisTickPen.Thickness / 2)));
                    }
            }

            _heatMapGraph.Render(drawingVisual);
        }

        private void DrawHeatMap()
        {
            ClearRenderTargetBitmap(_heatMap, Brushes.Black);
            if (_sensorsToUse.Count == 0)
            {
                Common.ShowMessageBox("Can't render", "You haven't selected any sensors to render the heatmap from",
                                      false, true);
                return;
            }

            var width = _heatMap.Width;
            var height = _heatMap.Height;

            var depths = _sensorsToUse.Select(x => x.Elevation).Distinct().OrderBy(x => x).ToArray();

            MinDepth = depths.Min();
            MaxDepth = depths.Max();


            if (!SpecifyRadiusEnabled)
                Radius = (float)height / (depths.Count() - 1) / 2;
            Debug.Print("Radius: {0}", Radius);

            var depthRange = MaxDepth - MinDepth;

            var heightMultiplier = height / (depths.Count() - 1);

            Debug.Print("Depth Min {0} Max {1} Range {2} Multiplier {3}", MinDepth, MaxDepth, depthRange, heightMultiplier);

            var timeStamps = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Key).ToArray();

            if (!SpecifyMinTimestampEnabled)
                MinTimestamp = timeStamps.Min();
            if (!SpecifyMaxTimestampEnabled)
                MaxTimestamp = timeStamps.Max();

            //No longer need timestamps
            timeStamps = null;

            var timeRange = MaxTimestamp - MinTimestamp;

            var widthMultiplier = width / timeRange.TotalMinutes;

            Debug.Print("Time Min {0} Max {1} Range {2} Multiplier {3}", MinTimestamp, MaxTimestamp, timeRange, widthMultiplier);

            if (!SpecifySamplingRateEnabled)
                SamplingRate = (int)(1 / widthMultiplier) * 2;

            var values = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Value).ToArray();

            if (!SpecifyMinValueEnabled)
                MinValue = values.Min();
            if (!SpecifyMaxValueEnabled)
                MaxValue = values.Max();

            values = null;

            var valuesRange = MaxValue - MinValue;

            var valuesMultiplier = 255 / valuesRange;

            Debug.Print("Values Min {0} Max {1} Range {2} Multiplier {3}", MinValue, MaxValue, valuesRange, valuesMultiplier);
            _depths = new List<DepthYValue>();
            foreach (var sensor in _sensorsToUse)
            {
                var y = (Array.IndexOf(depths, sensor.Elevation) * heightMultiplier);
                _depths.Add(new DepthYValue(sensor.Elevation, y));
                foreach (var timeStamp in sensor.CurrentState.Values.OrderBy(x => x.Key).Where((x, index) => index % SamplingRate == 0))
                {
                    var intensity = (byte)((timeStamp.Value - MinValue) * valuesMultiplier);
                    var radialGradientBrush = new RadialGradientBrush();
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(intensity, 255, 255, 255), 0.0));
                    radialGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
                    radialGradientBrush.Freeze();

                    var x = ((timeStamp.Key - MinTimestamp).TotalMinutes * widthMultiplier) - Radius;

                    //Debug.Print("Point Timestamp {0} Value {1} Depth {2} Intensity {3} x {4} y {5}", timeStamp.Key, timeStamp.Value, sensor.Depth, intensity, x, y - Radius);

                    var drawingVisual = new DrawingVisual();
                    using (var context = drawingVisual.RenderOpen())
                    {
                        context.DrawRectangle(radialGradientBrush, null, new Rect(x, y - Radius, Radius * 2, Radius * 2));
                    }
                    _heatMap.Render(drawingVisual);
                }
            }
        }

        private RenderTargetBitmap DrawHeatKey(int width, int height)
        {
            var heatKeyBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            ClearRenderTargetBitmap(heatKeyBitmap, Brushes.Black);
            var brush = new LinearGradientBrush();
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 1));
            brush.Freeze();
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(brush, null, new Rect(0, 0, heatKeyBitmap.PixelWidth, heatKeyBitmap.PixelHeight));
            }
            heatKeyBitmap.Render(visual);
            return heatKeyBitmap;
        }

        private static void ClearRenderTargetBitmap(RenderTargetBitmap renderTargetBitmap, Brush clearingBrush)
        {
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.DrawRectangle(clearingBrush, null, new Rect(0, 0, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight));
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
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 5, 0), 0.99d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 10, 0), 0.98d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 15, 0), 0.97d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 25, 0), 0.96d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 30, 0), 0.95d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 35, 0), 0.94d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 45, 0), 0.93d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 50, 0), 0.92d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 60, 0), 0.91d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 65, 0), 0.90d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 70, 0), 0.89d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 75, 0), 0.88d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 80, 0), 0.87d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 90, 0), 0.86d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 95, 0), 0.85d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 100, 0), 0.84d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 105, 0), 0.83d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 120, 0), 0.82d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 135, 0), 0.81d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 145, 0), 0.80d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 160, 0), 0.79d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 180, 0), 0.78d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 200, 0), 0.77d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 225, 0), 0.76d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 245, 0), 0.75d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 0.74d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(240, 255, 0), 0.73d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(230, 255, 0), 0.72d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(220, 255, 0), 0.71d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(205, 255, 0), 0.70d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(195, 255, 0), 0.69d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(180, 255, 0), 0.68d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(175, 255, 0), 0.67d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(165, 255, 0), 0.66d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(155, 255, 0), 0.65d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(140, 255, 0), 0.64d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(130, 255, 0), 0.63d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(115, 255, 0), 0.62d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(105, 255, 0), 0.61d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(90, 255, 0), 0.60d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(80, 255, 0), 0.59d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(70, 255, 0), 0.58d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(55, 255, 0), 0.57d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(35, 255, 0), 0.56d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(20, 255, 0), 0.55d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(10, 255, 0), 0.54d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 0.53d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 10), 0.52d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 25), 0.51d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 45), 0.50d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 60), 0.49d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 70), 0.48d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 85), 0.47d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 100), 0.46d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 110), 0.45d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 125), 0.44d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 135), 0.43d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 150), 0.42d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 165), 0.41d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 175), 0.40d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 190), 0.39d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 200), 0.38d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 210), 0.37d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 220), 0.36d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 235), 0.35d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 240), 0.34d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 250), 0.33d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.32d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 230, 255), 0.31d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 220, 255), 0.30d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 205, 255), 0.29d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 190, 255), 0.28d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 180, 255), 0.27d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 170, 255), 0.26d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 150, 255), 0.25d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 130, 255), 0.24d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 105, 255), 0.23d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 95, 255), 0.22d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 80, 255), 0.21d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 70, 255), 0.20d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 55, 255), 0.19d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 45, 255), 0.18d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 30, 255), 0.17d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 20, 255), 0.16d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 0.15d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 240), 0.14d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 230), 0.13d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 220), 0.12d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 200), 0.11d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 180), 0.10d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 160), 0.09d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 150), 0.08d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 140), 0.07d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 120), 0.06d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 100), 0.05d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 80), 0.04d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 60), 0.03d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 40), 0.02d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 20), 0.01d));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.0d));
            brush.Freeze();
            return brush;
        }

        private void DisableFeatures()
        {
            ApplicationCursor = Cursors.Wait;
            FeaturesEnabled = false;
        }

        private void EnableFeatures()
        {
            ApplicationCursor = Cursors.Arrow;
            FeaturesEnabled = true;
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
