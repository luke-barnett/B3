using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Zoom behaviour for graph
    /// </summary>
    class CustomZoomBehaviour : BehaviourBase
    {
        private readonly ZoomRectangle _zoomRectangle = new ZoomRectangle();
        private bool _leftMouseDown;
        private Point _firstPosition;

        public CustomZoomBehaviour()
            : base("Zoom Behaviour")
        {
            _zoomRectangle.Visibility = Visibility.Visible;
        }

        protected override void Init()
        {
            //Add it to the container
            BehaviourContainer.Children.Add(_zoomRectangle);
        }

        public override void DeInit()
        {
            //Ensure that we remove the items we have added
            if (BehaviourContainer != null && BehaviourContainer.Children.Contains(_zoomRectangle))
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
            if (Chart.XAxis.ActualRange == null || Chart.YAxis.ActualRange == null || !BehaviourContainer.CaptureMouse() || Keyboard.Modifiers == ModifierKeys.Shift)
                return;

            _leftMouseDown = true;
            _firstPosition = position;

            //Set the up the zoom rectangle
            _zoomRectangle.SetValue(Canvas.LeftProperty, position.X);
            _zoomRectangle.SetValue(Canvas.TopProperty, position.Y);
            _zoomRectangle.Width = 0;
            _zoomRectangle.Height = 0;
            if (_zoomRectangle.Border != null)
            {
                _zoomRectangle.Border.Background = _zoomRectangle.Background;
                _zoomRectangle.Border.BorderBrush = _zoomRectangle.Foreground;
            }

            //Make it visible
            _zoomRectangle.Visibility = Visibility.Visible;
        }

        public override void MouseMove(Point position)
        {
            if (!_leftMouseDown)
                return;
            position = EnsurePointIsOnChart(position);

            ChangeZoomRectangle(position);
        }

        public override void MouseLeftButtonUp(Point position)
        {
            if (!_leftMouseDown)
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
            if (Keyboard.Modifiers != ModifierKeys.Shift)
                RequestZoom(_firstPosition, position);
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
            }
            else
            {
                _zoomRectangle.Width = _firstPosition.X - position.X;
                _zoomRectangle.SetValue(Canvas.LeftProperty, position.X);
            }

            if (position.Y > _firstPosition.Y)
            {
                _zoomRectangle.Height = position.Y - _firstPosition.Y;
            }
            else
            {
                _zoomRectangle.Height = _firstPosition.Y - position.Y;
                _zoomRectangle.SetValue(Canvas.TopProperty, position.Y);
            }
        }

        private void RequestZoom(Point firstPoint, Point secondPoint)
        {
            var x1 = (DateTime)Chart.XAxis.GetRenderPositionAsDataValueWithoutZoom(firstPoint.X);
            var x2 = (DateTime)Chart.XAxis.GetRenderPositionAsDataValueWithoutZoom(secondPoint.X);
            var y1 = (Double)Chart.YAxis.GetRenderPositionAsDataValueWithoutZoom(firstPoint.Y);
            var y2 = (Double)Chart.YAxis.GetRenderPositionAsDataValueWithoutZoom(secondPoint.Y);

            OnZoomRequested(this, new ZoomRequestedArgs(x1, x2, (float)y1, (float)y2));
        }

        private void OnZoomRequested(object o, ZoomRequestedArgs e)
        {
            if (ZoomRequested != null)
                ZoomRequested(o, e);
        }
    }

    public class ZoomRequestedArgs : EventArgs
    {
        public readonly DateTime UpperX;
        public readonly DateTime LowerX;
        public readonly double UpperY;
        public readonly double LowerY;

        public ZoomRequestedArgs(DateTime x1, DateTime x2, float y1, float y2)
        {
            if (x1 > x2)
            {
                UpperX = x1;
                LowerX = x2;
            }
            else
            {
                UpperX = x2;
                LowerX = x1;
            }
            if (y1 > y2)
            {
                UpperY = y1;
                LowerY = y2;
            }
            else
            {
                UpperY = y2;
                LowerY = y1;
            }
            Debug.Print("New Zoom Request Made: Date Range {0} - {1} , ValueRange {2} - {3}", LowerX, UpperX, LowerY, UpperY);
        }
    }

    public delegate void ZoomRequested(object o, ZoomRequestedArgs e);

    public delegate void ZoomResetRequested(object o);
}
