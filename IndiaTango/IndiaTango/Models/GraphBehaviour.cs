using System;
using System.Windows;
using System.Windows.Controls;
using Visiblox.Charts;
using System.ComponentModel;

namespace IndiaTango.Models
{
    class GraphBehaviour : BehaviourBase, INotifyPropertyChanged
    {
        private const double PATH_MAX_SIZE = 32000;

        private ZoomRectangle _zoomRectangle = new ZoomRectangle();
        private bool _zooming;
        private bool _leftMouseDown;
        private Point _leftMouseDownPosition;

        public GraphBehaviour() : base("Custom Graph Behaviour")
        {
        }

        protected override void Init()
        {
            _zoomRectangle.Visibility = Visibility.Collapsed;
            BehaviourContainer.Children.Add(_zoomRectangle);
        }

        public override void DeInit()
        {
            if (BehaviourContainer != null && BehaviourContainer.Children.Contains(_zoomRectangle))
                BehaviourContainer.Children.Remove(_zoomRectangle);
        }

        public override void MouseLeftButtonDown(Point position)
        {
            #region pointFinding

            IDataPoint closestX = null;
            foreach(IDataPoint point in Chart.Series[0].DataSeries)
            {
                if (closestX == null)
                    closestX = point;
                else if (Math.Abs(Chart.Series[0].GetPointRenderPosition(point).X - position.X) < Math.Abs(Chart.Series[0].GetPointRenderPosition(closestX).X - position.X))
                    closestX = point;
            }
            if(closestX != null)
                System.Diagnostics.Debug.Print("X:{0} Y:{1} [{2}]", closestX.X, closestX.Y, position);

            #endregion

            if(BehaviourContainer.CaptureMouse() && !_zooming)
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

            double xPosOne = (position.X);
            double xPosTwo = (_leftMouseDownPosition.X);

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
            //Fire event to reset zoom
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

        #endregion
    }
}
