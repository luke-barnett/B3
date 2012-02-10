using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Annotates values based on dates
    /// </summary>
    class DateAnnotationBehaviour : BehaviourBase
    {
        private Canvas _annotationCanvas;
        private DateTime _shownTime = DateTime.Now;
        private string _value;
        private TextBlock _textBlock;
        private int _dataInterval = 15;
        private const int HeightInterval = 15;

        public DateAnnotationBehaviour()
            : base("DateAnnotator")
        {
        }

        private DateTime Time
        {
            get { return _shownTime; }
            set
            {
                _shownTime = value;
                _textBlock.Text = _shownTime.ToString("yyyy/MM/dd HH:mm") + _value;
            }
        }

        private string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _textBlock.Text = _shownTime.ToString("yyyy/MM/dd HH:mm") + _value;
            }
        }

        protected override void Init()
        {
            _annotationCanvas = new Canvas
                                  {
                                      Visibility = Visibility.Collapsed,
                                      Width = 100,
                                      Height = HeightInterval,
                                      Background = Brushes.Honeydew,
                                      Opacity = 0.7f
                                  };
            _textBlock = new TextBlock();
            _annotationCanvas.Children.Add(_textBlock);
            BehaviourContainer.Children.Add(_annotationCanvas);

            PropertyChanged += DateAnnotationBehaviourPropertyChanged;
        }

        void DateAnnotationBehaviourPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsEnabled" && !IsEnabled)
                _annotationCanvas.Visibility = Visibility.Hidden;
        }

        public override void DeInit()
        {
            if (BehaviourContainer.Children.Contains(_annotationCanvas))
                BehaviourContainer.Children.Remove(_annotationCanvas);

            PropertyChanged -= DateAnnotationBehaviourPropertyChanged;
        }

        public override void MouseMove(Point position)
        {
            if (position.X + 15 + _annotationCanvas.Width > BehaviourContainer.ActualWidth)
                _annotationCanvas.SetValue(Canvas.LeftProperty, position.X - (_annotationCanvas.Width + 15));
            else
                _annotationCanvas.SetValue(Canvas.LeftProperty, position.X + 15);
            
            _annotationCanvas.Height = HeightInterval;
            Value = string.Empty;

            if (Chart.XAxis.ActualRange == null)
            {
                _annotationCanvas.Visibility = Visibility.Hidden;
                return;
            }
                

            Time = (DateTime)Chart.XAxis.GetRenderPositionAsDataValueWithoutZoom(position.X);
            Time = Time.Round(new TimeSpan(0, _dataInterval, 0));

            var graphed = false;
            foreach (var series in Chart.Series)
            {
                var dataPoints = series.DataSeries as DataSeries<DateTime, float>;
                if (dataPoints == null) continue;

                foreach (var dataPoint in dataPoints.Where(dataPoint => dataPoint.X == Time))
                {
                    graphed = true;
                    Value += string.Format("\r\n{0} [{1}]", dataPoint.Y, series.DataSeries.Title);
                    _annotationCanvas.Height += HeightInterval;
                    break;
                }
            }

            if(position.Y + _annotationCanvas.Height > BehaviourContainer.ActualHeight)
                _annotationCanvas.SetValue(Canvas.TopProperty, position.Y - _annotationCanvas.Height);
            else
                _annotationCanvas.SetValue(Canvas.TopProperty, position.Y);

            _annotationCanvas.Visibility = graphed ? Visibility.Visible : Visibility.Hidden;
        }

        public int DataInterval
        {
            set { _dataInterval = value; }
        }
    }
}
