using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class DateAnnotationBehaviour : BehaviourBase
    {
        private Canvas _annotationCanvas;
        private DateTime _shownTime = DateTime.Now;
        private TextBlock _textBlock;

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
                _textBlock.Text = _shownTime.ToString("yyyy/MM/dd HH:mm:ss");
            }
        }

        protected override void Init()
        {
            _annotationCanvas = new Canvas
                                  {
                                      Visibility = Visibility.Collapsed,
                                      Width = 100,
                                      Height = 30,
                                      Background = Brushes.DarkKhaki
                                  };
            _textBlock = new TextBlock();
            _annotationCanvas.Children.Add(_textBlock);
            BehaviourContainer.Children.Add(_annotationCanvas);
        }

        public override void DeInit()
        {
            if (BehaviourContainer.Children.Contains(_annotationCanvas))
                BehaviourContainer.Children.Remove(_annotationCanvas);
        }

        public override void MouseMove(Point position)
        {
            _annotationCanvas.SetValue(Canvas.LeftProperty, position.X);
            _annotationCanvas.SetValue(Canvas.TopProperty, position.Y);

            _annotationCanvas.Visibility = Visibility.Visible;
            Time = DateTime.Now;
        }
    }
}
