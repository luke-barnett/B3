using System.Collections.Generic;
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
    /// <summary>
    /// Annotates calibrations on the graph
    /// </summary>
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
            if (xAxis == null)
                throw new Exception();

            Chart.Series.CollectionChanged += SeriesCollectionChanged;
            xAxis.SizeChanged += OnSizeChanged;
            PropertyChanged += OnPropertyChanged;

            var grid = xAxis.Parent as Grid;

            if (grid != null)
                grid.Children.Add(_canvas);
        }

        public override void DeInit()
        {
            var xAxis = (Chart.XAxis as DateTimeAxis);
            if (xAxis == null)
                throw new Exception();

            Chart.Series.CollectionChanged -= SeriesCollectionChanged;
            xAxis.SizeChanged -= OnSizeChanged;
            PropertyChanged -= OnPropertyChanged;

            RemoveAllAnnotations();
            var grid = xAxis.Parent as Grid;

            if (grid != null)
                grid.Children.Remove(_canvas);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "IsEnabled")
            {
                if (IsEnabled)
                    DrawAnnotations();
                else
                    RemoveAllAnnotations();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            if (IsEnabled)
                DrawAnnotations();
        }

        private void SeriesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (IsEnabled)
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
                                                  "[{0}] {1}",
                                                  sensor.Name, calibration),
                                          Stroke = Brushes.Chartreuse,
                                          Fill = new SolidColorBrush(sensor.Colour)
                                      };
                    ellipse.SetValue(Canvas.TopProperty, 0d);
                    ellipse.SetValue(Canvas.LeftProperty, xAxis.GetDataValueAsRenderPositionWithoutZoom(calibration.TimeStamp) - ellipse.Width / 2);
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
