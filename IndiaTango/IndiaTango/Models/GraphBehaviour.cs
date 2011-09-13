using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class GraphBehaviour : BehaviourBase
    {
        private const double PATH_MAX_SIZE = 200000;

        private readonly ZoomRectangle _zoomRectangle = new ZoomRectangle();
        private readonly Canvas _background;
        private bool _leftMouseDown;
        private Point _leftMouseDownPosition;

        public event ZoomRequested ZoomRequested;
        public event ZoomResetRequested ZoomResetRequested;

        public GraphBehaviour(Canvas background) : base("Custom Graph Behaviour")
        {
            _background = background;
        }

        protected override void Init()
        {
            _zoomRectangle.Visibility = Visibility.Collapsed;
            BehaviourContainer.Children.Add(_zoomRectangle);

            _background.Background = Brushes.Red;
            _background.Opacity = 0.15;
            _background.SetValue(Canvas.LeftProperty, 0.0);
            _background.SetValue(Canvas.TopProperty, 0.0);
            _background.Width = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
            _background.Height = Double.IsNaN(Chart.ActualHeight) ? 400 : Chart.ActualHeight;
            BehaviourContainer.Children.Add(_background);
            BehaviourContainer.SizeChanged += (o, e) =>
            {
                _background.Width = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
                _background.Height = Double.IsNaN(Chart.ActualHeight) ? 400 : Chart.ActualHeight;
            };
        }

        public override void DeInit()
        {
            if (BehaviourContainer != null && BehaviourContainer.Children.Contains(_zoomRectangle))
                BehaviourContainer.Children.Remove(_zoomRectangle);

            if (BehaviourContainer != null && BehaviourContainer.Children.Contains(_background))
                BehaviourContainer.Children.Remove(_background);
        }

        public override void MouseLeftButtonDown(Point position)
        {
            #region pointFinding

            var closestX = FindClosestPoint(position);
            if(closestX != null)
                Debug.Print("X:{0} Y:{1} [{2}]", closestX.X, closestX.Y, position);

            #endregion

            if(BehaviourContainer.CaptureMouse())
            {
                _leftMouseDown = true;
                _leftMouseDownPosition = position;
                _zoomRectangle.SetValue(Canvas.LeftProperty, position.X);
                _zoomRectangle.SetValue(Canvas.TopProperty, 0.0);
                _zoomRectangle.Width = 0;
                _zoomRectangle.Height = Double.IsNaN(Chart.ActualHeight) ? 400 : Chart.ActualHeight;
                _zoomRectangle.Visibility = Visibility.Visible;
            }
        }

        public override void MouseMove(Point position)
        {
            position = EnsurePointIsOnChart(position);
            if (_leftMouseDown)
            {
                ChangeZoomRectangle(position);
                double xPosOne = (position.X);
                double xPosTwo = (_leftMouseDownPosition.X);

                if (CheckZoomIsPossible(xPosOne, xPosTwo))
                {
                    if (_zoomRectangle.Border != null)
                    {
                        _zoomRectangle.Border.Background = _zoomRectangle.Background;
                        _zoomRectangle.Border.BorderBrush = _zoomRectangle.Foreground;
                    }
                }
                else
                {
                    if (_zoomRectangle.Border != null)
                    {
                        _zoomRectangle.Border.Background = _zoomRectangle.NotZoomableBackground;
                        _zoomRectangle.Border.BorderBrush = _zoomRectangle.NotZoomableForeground;
                    }
                }
            }
        }

        public override void MouseLeftButtonUp(Point position)
        {
            position = EnsurePointIsOnChart(position);
            if (!_leftMouseDown)
                return;

            _leftMouseDown = false;
            BehaviourContainer.ReleaseMouseCapture();
            _zoomRectangle.Visibility = Visibility.Collapsed;

            if (Math.Abs(position.X - _leftMouseDownPosition.X) < 2)
                return;

            var positionOne = FindClosestPoint(_leftMouseDownPosition);
            var positionTwo = FindClosestPoint(position);

            if (CheckZoomIsPossible(position.X, _leftMouseDownPosition.X) && positionOne != positionTwo)
            {
                Debug.Print("{0} {1}", positionOne, positionTwo);
                RequestZoom(positionOne, positionTwo);
            }
        }

        public override void LostMouseCapture()
        {
            _leftMouseDown = false;
            _zoomRectangle.Width = 0;
            _zoomRectangle.Height = 0;
            _zoomRectangle.Visibility = Visibility.Collapsed;
        }

        public override void MouseLeftButtonDoubleClick(Point position)
        {
            if (ZoomResetRequested != null)
                ZoomResetRequested(this);
        }

        public void RefreshVisual()
        {
            if(BehaviourContainer != null)
                BehaviourContainer.InvalidateVisual();
        }

        #region private methods

        private bool CheckZoomIsPossible(double xPosOne, double xPosTwo)
        {
            if (xPosOne == xPosTwo)
            {
                xPosTwo = xPosTwo + 0.1;
            }

            Zoom zoom = Chart.XAxis.GetZoom(xPosTwo, xPosOne);
            if (1 / zoom.Scale < FindMaximumScaleForZoom())
            {
                return true;
            }
            return false;
        }

        private void ChangeZoomRectangle(Point position)
        {
            if (position.X > _leftMouseDownPosition.X)
            {
                _zoomRectangle.Width = position.X - _leftMouseDownPosition.X;
                _zoomRectangle.SetValue(Canvas.LeftProperty, _leftMouseDownPosition.X);
            }
            else
            {
                _zoomRectangle.Width = _leftMouseDownPosition.X - position.X;
                _zoomRectangle.SetValue(Canvas.LeftProperty, position.X);
            }
        }

        private Point EnsurePointIsOnChart(Point position)
        {
            var xPos = Math.Max(0, Math.Min(position.X, BehaviourContainer.ActualWidth));
            var yPos = Math.Max(0, Math.Min(position.Y, BehaviourContainer.ActualHeight));
            return new Point(xPos, yPos);
        }

        private double FindMaximumScaleForZoom()
        {
            double chartWidth = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
            double maximumWidthScale = PATH_MAX_SIZE / chartWidth;
            return maximumWidthScale;
        }

        private IDataPoint FindClosestPoint(Point position)
        {
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
            OnZoomRequested(this, new ZoomRequestedArgs(firstPoint, secondPoint));
        }

        private void OnZoomRequested(object o, ZoomRequestedArgs e)
        {
            if (ZoomRequested != null)
                ZoomRequested(o, e);
        }

        #endregion
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
