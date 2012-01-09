using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IndiaTango.ViewModels;
using Visiblox.Charts;
using System;

namespace IndiaTango.Models
{
    public class CalibrationAnnotatorBehaviour : BehaviourBase
    {
        private readonly MainWindowViewModel _viewModel;
        private List<UIElement> _annotations;

        public CalibrationAnnotatorBehaviour(MainWindowViewModel viewModel)
            : base("CalibrationAnnotator")
        {
            _viewModel = viewModel;
            _annotations = new List<UIElement>();
        }

        protected override void Init()
        {
            Chart.Series.CollectionChanged += SeriesCollectionChanged;
            Chart.SizeChanged += ChartOnSizeChanged;
            Chart.XAxis.ValueConversionChanged += XAxisOnValueConversionChanged;
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if(propertyChangedEventArgs.PropertyName == "IsEnabled")
            {
                if(IsEnabled)
                    DrawAnnotations();
                else
                    RemoveAllAnnotations();
            }
        }

        public override void DeInit()
        {
            Chart.Series.CollectionChanged -= SeriesCollectionChanged;
            Chart.SizeChanged -= ChartOnSizeChanged;
            Chart.XAxis.ValueConversionChanged -= XAxisOnValueConversionChanged;
            RemoveAllAnnotations();
        }

        private void XAxisOnValueConversionChanged(object sender, EventArgs eventArgs)
        {
            DrawAnnotations();
        }

        private void ChartOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            DrawAnnotations();
        }

        private void SeriesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DrawAnnotations();
        }

        public void DrawAnnotations()
        {
            RemoveAllAnnotations();
            var xAxis = Chart.XAxis as DateTimeAxis;
            if (xAxis == null || xAxis.ActualRange == null) return;
            foreach (var sensor in _viewModel.SensorsToCheckMethodsAgainst)
            {
                foreach (var calibration in sensor.Calibrations.Where(calibration => calibration.TimeStamp >= xAxis.ActualRange.EffectiveMinimum && calibration.TimeStamp <= xAxis.ActualRange.EffectiveMaximum))
                {
                    var ellipse = new Ellipse
                                      {
                                          Width = 10,
                                          Height = 10,
                                          ToolTip =
                                              string.Format(
                                                  "Calibration: {0} Pre Low: {1} Pre High: {2} Post Low: {3} Post High: {4}",
                                                  calibration.TimeStamp, calibration.PreLow, calibration.PreHigh,
                                                  calibration.PostLow, calibration.PostHigh),
                                          Stroke = Brushes.Chartreuse,
                                          Fill = Brushes.LightPink
                                      };
                    ellipse.SetValue(Canvas.TopProperty, 15d);
                    ellipse.SetValue(Canvas.LeftProperty, xAxis.GetDataValueAsRenderPositionWithoutZoom(calibration.TimeStamp) - ellipse.Width/2);
                    _annotations.Add(ellipse);
                }
            }
            foreach (var annotation in _annotations)
            {
                BehaviourContainer.Children.Add(annotation);
            }
        }

        public void RemoveAllAnnotations()
        {
            foreach (var annotation in _annotations)
            {
                BehaviourContainer.Children.Remove(annotation);
            }
            _annotations = new List<UIElement>();
        }
    }
}
