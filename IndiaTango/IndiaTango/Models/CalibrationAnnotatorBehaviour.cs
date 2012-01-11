﻿using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly Canvas _canvas;

        public CalibrationAnnotatorBehaviour(MainWindowViewModel viewModel)
            : base("CalibrationAnnotator")
        {
            _viewModel = viewModel;
            _annotations = new List<UIElement>();
            _canvas = new Canvas();
        }

        protected override void Init()
        {
            var xAxis = (Chart.XAxis as DateTimeAxis);
            if(xAxis == null)
                throw new Exception();

            Chart.Series.CollectionChanged += SeriesCollectionChanged;
            Chart.SizeChanged += ChartOnSizeChanged;
            Chart.XAxis.ValueConversionChanged += XAxisOnValueConversionChanged;
            xAxis.ValueConversionChanged += XAxisOnValueConversionChanged;
            xAxis.SizeChanged += OnSizeChanged;
            PropertyChanged += OnPropertyChanged;

            var grid = xAxis.Parent as Grid;

            if (grid != null)
                grid.Children.Add(_canvas);
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
            var xAxis = (Chart.XAxis as DateTimeAxis);
            if (xAxis == null)
                throw new Exception();

            Chart.Series.CollectionChanged -= SeriesCollectionChanged;
            Chart.SizeChanged -= ChartOnSizeChanged;
            Chart.XAxis.ValueConversionChanged -= XAxisOnValueConversionChanged;
            xAxis.ValueConversionChanged -= XAxisOnValueConversionChanged;
            xAxis.SizeChanged -= OnSizeChanged;
            PropertyChanged -= OnPropertyChanged;

            RemoveAllAnnotations();
            var grid = xAxis.Parent as Grid;

            if (grid != null)
                grid.Children.Remove(_canvas);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            DrawAnnotations();
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
                                                  "[{5}] Calibration: {0} Pre Low: {1} Pre High: {2} Post Low: {3} Post High: {4}",
                                                  calibration.TimeStamp, calibration.PreLow, calibration.PreHigh,
                                                  calibration.PostLow, calibration.PostHigh, sensor.Name),
                                          Stroke = Brushes.Chartreuse,
                                          Fill = new SolidColorBrush(sensor.Colour),
                                          Opacity = 0.85d
                                      };
                    ellipse.SetValue(Canvas.TopProperty, xAxis.GetValue(Canvas.TopProperty));
                    ellipse.SetValue(Canvas.LeftProperty, xAxis.GetDataValueAsRenderPositionWithoutZoom(calibration.TimeStamp) - ellipse.Width/2);
                    _annotations.Add(ellipse);
                }
            }
            foreach (var annotation in _annotations)
            {
                _canvas.Children.Add(annotation);
            }
        }

        public void RemoveAllAnnotations()
        {
            foreach (var annotation in _annotations)
            {
                _canvas.Children.Remove(annotation);
            }
            _annotations = new List<UIElement>();
        }
    }
}