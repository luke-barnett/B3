using System;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Provides access to a background for the graph
    /// </summary>
    class GraphBackgroundBehaviour : BehaviourBase
    {
        private readonly Canvas _background;

        public GraphBackgroundBehaviour(Canvas background) : base("Graph Background Behaviour")
        {
            _background = background;
        }

        protected override void Init()
        {
            if (_background == null)
                return;

            //Set up background
            _background.Background = Brushes.Red;
            _background.Opacity = 0.15;
            _background.SetValue(Canvas.LeftProperty, 0.0);
            _background.SetValue(Canvas.TopProperty, 0.0);

            //Set initial width and height
            _background.Width = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
            _background.Height = Double.IsNaN(Chart.ActualHeight) ? 400 : Chart.ActualHeight;

            //Resize when the chart is resized
            BehaviourContainer.SizeChanged += (o, e) =>
                                                  {
                                                      _background.Width = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
                                                      _background.Height = Double.IsNaN(Chart.ActualHeight) ? 400 : Chart.ActualHeight;
                                                  };

            //Refresh on visibility change
            _background.IsVisibleChanged += (o, e) => _background.InvalidateVisual();

            BehaviourContainer.Children.Add(_background);
        }

        public override void DeInit()
        {
            //Remove our footprint
            if (BehaviourContainer != null && BehaviourContainer.Children.Contains(_background))
                BehaviourContainer.Children.Remove(_background);
        }
    }
}
