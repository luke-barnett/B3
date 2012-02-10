using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IndiaTango.ViewModels;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Annotates changes on the graph
    /// </summary>
    public class ChangesAnnotatorBehaviour : BehaviourBase
    {
        private readonly MainWindowViewModel _viewModel;
        private List<UIElement> _annotations;
        private readonly Canvas _canvas;

        public ChangesAnnotatorBehaviour(MainWindowViewModel viewModel)
            : base("ChangesAnnotator")
        {
            _viewModel = viewModel;
            _annotations = new List<UIElement>();
            _canvas = new Canvas();
        }

        protected override void Init()
        {
            var xAxis = (Chart.XAxis as DateTimeAxis);
            if (xAxis == null)
                throw new Exception("Designed to work with DateTimeAxis");

            Chart.Series.CollectionChanged += SeriesOnCollectionChanged;
            xAxis.SizeChanged += XAxisOnSizeChanged;
            PropertyChanged += OnPropertyChanged;

            var grid = xAxis.Parent as Grid;

            if (grid != null)
                grid.Children.Add(_canvas);
        }

        public override void DeInit()
        {
            var xAxis = (Chart.XAxis as DateTimeAxis);
            if (xAxis == null)
                throw new Exception("Designed to work with DateTimeAxis");

            Chart.Series.CollectionChanged -= SeriesOnCollectionChanged;
            xAxis.SizeChanged -= XAxisOnSizeChanged;
            PropertyChanged -= OnPropertyChanged;

            RemoveAllAnnotations();
            var grid = xAxis.Parent as Grid;

            if (grid != null)
                grid.Children.Remove(_canvas);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != "IsEnabled") return;

            if (IsEnabled)
                DrawAnnotations();
            else
                RemoveAllAnnotations();
        }

        private void SeriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (IsEnabled)
                DrawAnnotations();
        }

        private void XAxisOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
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
                var lastLeft = -1d;
                foreach (var source in sensor.CurrentState.Changes.Where(change => change.Key >= xAxis.ActualRange.EffectiveMinimum && change.Key <= xAxis.ActualRange.EffectiveMaximum).OrderBy(x => x.Key))
                {
                    var changes = source.Value.Aggregate("",
                                                     (current, next) =>
                                                     string.Format("\r\n{0}",
                                                                   ChangeReason.ChangeReasons.FirstOrDefault(
                                                                       x => x.ID == next)));
                    var rect = new Rectangle
                                   {
                                       Width = 5,
                                       Height = 5,
                                       ToolTip =
                                       string.Format("[{0}] {1}{2}", sensor.Name, source.Key, changes),
                                       StrokeThickness = 0d,
                                       Fill = new SolidColorBrush(sensor.Colour),
                                       Opacity = 0.7d
                                   };
                    rect.SetValue(Canvas.TopProperty, 20d);
                    rect.SetValue(Canvas.LeftProperty, xAxis.GetDataValueAsRenderPositionWithoutZoom(source.Key) - rect.Width / 2);
                    if ((double)rect.GetValue(Canvas.LeftProperty) - lastLeft > 2d)
                    {
                        _annotations.Add(rect);
                        lastLeft = (double)rect.GetValue(Canvas.LeftProperty);
                    }

                }
            }
            Debug.Print("There are {0} annotations to draw", _annotations.Count);
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
