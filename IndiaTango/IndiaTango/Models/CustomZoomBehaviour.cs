using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class CustomZoomBehaviour : BehaviourBase
    {
        private readonly ZoomRectangle _zoomRectangle = new ZoomRectangle();
        private bool _leftMouseDown;
        private Point _firstPosition;

        public CustomZoomBehaviour() : base("Zoom Behaviour")
        {
        }

        protected override void Init()
        {
            //Set up the zoom rectangle for use
            _zoomRectangle.Visibility = Visibility.Visible;
            //Add it to the container
            BehaviourContainer.Children.Add(_zoomRectangle);
        }

        public override void DeInit()
        {
            //Ensure that we remove the items we have added
            if(BehaviourContainer != null && BehaviourContainer.Children.Contains(_zoomRectangle))
                BehaviourContainer.Children.Remove(_zoomRectangle);
        }

        #region Events

        /// <summary>
        /// Fired when a zoom has been requested by the user
        /// </summary>
        public event ZoomRequested ZoomRequested;

        /// <summary>
        /// Fired when the zoom has been requested to be reset
        /// </summary>
        public event ZoomResetRequested ZoomResetRequested;

        #endregion

        public override void MouseLeftButtonDown(Point position)
        {
            if(BehaviourContainer.CaptureMouse())
            {
                _leftMouseDown = true;
                _firstPosition = position;

                //Set the up the zoom rectangle
                _zoomRectangle.SetValue(Canvas.LeftProperty, position.X);
                _zoomRectangle.SetValue(Canvas.TopProperty, 0.0);
                _zoomRectangle.Width = 0;
                _zoomRectangle.Height = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
                if (_zoomRectangle.Border != null)
                {
                    _zoomRectangle.Border.Background = _zoomRectangle.Background;
                    _zoomRectangle.Border.BorderBrush = _zoomRectangle.Foreground;
                }

                //Make it visible
                _zoomRectangle.Visibility = Visibility.Visible;
            }
        }

        public override void MouseMove(Point position)
        {
            if(!_leftMouseDown)
                return;
            position = EnsurePointIsOnChart(position);

            ChangeZoomRectangle(position);
        }

        public override void MouseLeftButtonUp(Point position)
        {
            if(!_leftMouseDown)
                return;

            position = EnsurePointIsOnChart(position);

            //Mouse button is no longer down
            _leftMouseDown = false;
            //Reset the zoom rectangle to hidden
            _zoomRectangle.Visibility = Visibility.Collapsed;

            BehaviourContainer.ReleaseMouseCapture();

            //If we haven't really gone far enough to bother stop now
            if (Math.Abs(position.X - _firstPosition.X) < 2)
                return;

            var firstPoint = FindClosestPoint(_firstPosition);
            var secondPoint = FindClosestPoint(position);

            //As long as they aren't the same point request a zoom
            if(firstPoint != secondPoint && firstPoint != null && secondPoint != null)
            {
                RequestZoom(firstPoint, secondPoint);
            }
        }

        public override void MouseLeftButtonDoubleClick(Point position)
        {
            if (ZoomResetRequested != null)
                ZoomResetRequested(this);
        }

        public override void LostMouseCapture()
        {
            //Lost capture hide!!
            _leftMouseDown = false;
            _zoomRectangle.Width = 0;
            _zoomRectangle.Height = 0;
            _zoomRectangle.Visibility = Visibility.Collapsed;
        }

        private Point EnsurePointIsOnChart(Point position)
        {
            var xPos = Math.Max(0, Math.Min(position.X, BehaviourContainer.ActualWidth));
            var yPos = Math.Max(0, Math.Min(position.Y, BehaviourContainer.ActualHeight));
            return new Point(xPos, yPos);
        }

        private void ChangeZoomRectangle(Point position)
        {
            if (position.X > _firstPosition.X)
            {
                _zoomRectangle.Width = position.X - _firstPosition.X;
                _zoomRectangle.SetValue(Canvas.LeftProperty, _firstPosition.X);
            }
            else
            {
                _zoomRectangle.Width = _firstPosition.X - position.X;
                _zoomRectangle.SetValue(Canvas.LeftProperty, position.X);
            }
        }

        private IDataPoint FindClosestPoint(Point position)
        {
            //If we have nothing to count from then we can't do anything
            if (Chart.Series.Count == 0)
                return null;

            IDataPoint closestX = null;
            foreach (IDataPoint point in Chart.Series[0].DataSeries)
            {
                if (closestX == null)
                    closestX = point;
                else if (Math.Abs(Chart.Series[0].GetPointRenderPosition(point).X - position.X) < Math.Abs(Chart.Series[0].GetPointRenderPosition(closestX).X - position.X))
                    closestX = point;
            }
            return closestX;
        }

        private void RequestZoom(IDataPoint firstPoint, IDataPoint secondPoint)
        {
            Debug.Print("Requesting a zoom between {0} and {1}", firstPoint, secondPoint);
            OnZoomRequested(this, new ZoomRequestedArgs(firstPoint, secondPoint));
        }

        private void OnZoomRequested(object o, ZoomRequestedArgs e)
        {
            if (ZoomRequested != null)
                ZoomRequested(o, e);
        }
    }

    public class ZoomRequestedArgs : EventArgs
    {
        public readonly IDataPoint FirstPoint;
        public readonly IDataPoint SecondPoint;

        public ZoomRequestedArgs(IDataPoint firstPoint, IDataPoint secondPoint)
        {
            FirstPoint = firstPoint;
            SecondPoint = secondPoint;
        }
    }

    public delegate void ZoomRequested(object o, ZoomRequestedArgs e);

    public delegate void ZoomResetRequested(object o);
}
