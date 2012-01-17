using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class HeatMapViewModel : BaseViewModel
    {
        private Canvas _heatMapCanvas;
        private readonly List<Sensor> _sensorsToUse;
        private List<Sensor> _availableSensors;

        public HeatMapViewModel()
        {
            _sensorsToUse = new List<Sensor>();
            _availableSensors = new List<Sensor>();
            PropertyChanged += OnPropertyChanged;
        }

        public Canvas HeatMapCanvas
        {
            get { return _heatMapCanvas; }
            set
            {
                _heatMapCanvas = value;
                NotifyOfPropertyChange(() => HeatMapCanvas);
            }
        }

        public List<Sensor> AvailableSensors
        {
            get { return _availableSensors; }
            set
            {
                _availableSensors = value;
                NotifyOfPropertyChange(() => AvailableSensors);
                _sensorsToUse.AddRange(AvailableSensors.Take(7));
            }
        }

        private bool CanDrawHeatMap
        {
            get { return HeatMapCanvas != null; }
        }

        #region Private Methods
        private void DrawHeatMap()
        {
            if (!CanDrawHeatMap) return;

            var width = HeatMapCanvas.ActualWidth;
            var height = HeatMapCanvas.ActualHeight;

            var depths = _sensorsToUse.Select(x => x.Depth).ToArray();

            var minDepth = depths.Min();
            var maxDepth = depths.Max();

            //No longer need depths
            depths = null;

            var depthRange = maxDepth - minDepth;

            var heightMultiplier = height / depthRange;


            var timeStamps = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Key).ToArray();

            var minTimeStamp = timeStamps.Min();
            var maxTimeStamp = timeStamps.Max();

            //No longer need timestamps
            timeStamps = null;

            var timeRange = maxTimeStamp - minTimeStamp;

            var widthMultiplier = width / timeRange.TotalMinutes;

            var values = _sensorsToUse.SelectMany(x => x.CurrentState.Values).Select(x => x.Value).ToArray();

            var minValue = values.Min();
            var maxValue = values.Max();

            values = null;

            var valuesRange = maxValue - minValue;

            var valuesMultiplier = 255 / valuesRange;

            const double ellipseRadius = 200d;

            HeatMapCanvas.Background = Brushes.Black;
            //Drawing Ellipses
            foreach (var sensor in _sensorsToUse)
            {
                foreach (var timeStamp in sensor.CurrentState.Values)
                {
                    var radialGradientBrush = new RadialGradientBrush(Color.FromArgb(((byte)((timeStamp.Value - minValue) * valuesMultiplier)), 0, 0, 0),
                                                                      Color.FromArgb(0, 0, 0, 0))
                                                  {
                                                      GradientOrigin = new Point(ellipseRadius, ellipseRadius),
                                                      Center = new Point(ellipseRadius, ellipseRadius),
                                                      RadiusX = ellipseRadius,
                                                      RadiusY = ellipseRadius
                                                  };

                    radialGradientBrush.Freeze();


                    var ellipse = new Ellipse
                                      {
                                          Width = ellipseRadius * 2,
                                          Height = ellipseRadius * 2,
                                          Fill = radialGradientBrush
                                      };
                    ellipse.SetValue(Canvas.TopProperty, height - ((sensor.Depth - minDepth) * heightMultiplier));
                    ellipse.SetValue(Canvas.LeftProperty, ((timeStamp.Key - minTimeStamp).TotalMinutes * widthMultiplier));
                    HeatMapCanvas.Children.Add(ellipse);
                }
            }
        }
        #endregion

        #region Event Handlers
        public void WindowLoaded(Canvas heatMapCanvas)
        {
            HeatMapCanvas = heatMapCanvas;
            DrawHeatMap();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {

        }
        #endregion
    }
}
