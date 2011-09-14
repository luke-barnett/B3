using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class CustomSelectionBehaviour : BehaviourBase
    {
        private readonly ZoomRectangle _selectionRectangle = new ZoomRectangle();
        private bool _leftMouseDown;
        private Point _firstPosition;

        public CustomSelectionBehaviour() : base("Selection Behaviour")
        {
        }

        protected override void Init()
        {
            _selectionRectangle.Visibility = Visibility.Collapsed;
            BehaviourContainer.Children.Add(_selectionRectangle);
        }

        public override void DeInit()
        {
            if(BehaviourContainer != null && BehaviourContainer.Children.Contains(_selectionRectangle))
                BehaviourContainer.Children.Remove(_selectionRectangle);
        }

        #region Events

        /// <summary>
        /// Fired when a selection is made
        /// </summary>
        public event SelectionMade SelectionMade;

        /// <summary>
        /// Fired when a selection is reset
        /// </summary>
        public event SelectionReset SelectionReset;

        #endregion

        public override void MouseLeftButtonDown(Point position)
        {
            if (BehaviourContainer.CaptureMouse())
            {
                _leftMouseDown = true;
                _firstPosition = position;

                //Set the up the zoom rectangle
                _selectionRectangle.SetValue(Canvas.LeftProperty, position.X);
                _selectionRectangle.SetValue(Canvas.TopProperty, 0.0);
                _selectionRectangle.Width = 0;
                _selectionRectangle.Height = Double.IsNaN(Chart.ActualWidth) ? 400 : Chart.ActualWidth;
                if (_selectionRectangle.Border != null)
                {
                    _selectionRectangle.Border.Background = _selectionRectangle.Background;
                    _selectionRectangle.Border.BorderBrush = _selectionRectangle.Foreground;
                }

                //Make it visible
                _selectionRectangle.Visibility = Visibility.Visible;
            }
        }

        public override void MouseMove(Point position)
        {
            if (!_leftMouseDown)
                return;
            position = EnsurePointIsOnChart(position);

            ChangeSelectionRectangle(position);
        }

        public override void MouseLeftButtonUp(Point position)
        {
            if (!_leftMouseDown)
                return;

            position = EnsurePointIsOnChart(position);

            _leftMouseDown = false;

            BehaviourContainer.ReleaseMouseCapture();

            if(Math.Abs(position.X - _firstPosition.X) < 2)
            {
                _selectionRectangle.Width = 0;
                _selectionRectangle.Height = 0;
            }

            var firstPoint = FindClosestPoint(_firstPosition);
            var secondPoint = FindClosestPoint(position);

            //As long as they aren't the same point request a zoom
            if (firstPoint != secondPoint && firstPoint != null && secondPoint != null)
            {
                MakeSelection(firstPoint, secondPoint);
            }
        }

        public override void MouseLeftButtonDoubleClick(Point position)
        {
            Debug.WriteLine("Reseting selection");
            _selectionRectangle.Width = 0;
            _selectionRectangle.Height = 0;
            _selectionRectangle.Visibility = Visibility.Collapsed;
            if (SelectionReset != null)
                SelectionReset(this);
        }

        public override void LostMouseCapture()
        {
            _leftMouseDown = false;
        }

        private Point EnsurePointIsOnChart(Point position)
        {
            var xPos = Math.Max(0, Math.Min(position.X, BehaviourContainer.ActualWidth));
            var yPos = Math.Max(0, Math.Min(position.Y, BehaviourContainer.ActualHeight));
            return new Point(xPos, yPos);
        }

        private void ChangeSelectionRectangle(Point position)
        {
            if (position.X > _firstPosition.X)
            {
                _selectionRectangle.Width = position.X - _firstPosition.X;
                _selectionRectangle.SetValue(Canvas.LeftProperty, _firstPosition.X);
            }
            else
            {
                _selectionRectangle.Width = _firstPosition.X - position.X;
                _selectionRectangle.SetValue(Canvas.LeftProperty, position.X);
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

        private void MakeSelection(IDataPoint firstPoint, IDataPoint secondPoint)
        {
            Debug.Print("A new selection made between {0} and {1}", firstPoint, secondPoint);
            if (SelectionMade != null)
                SelectionMade(this, new SelectionMadeArgs(firstPoint, secondPoint));
        }

    }

    public class SelectionMadeArgs : EventArgs
    {
        public readonly IDataPoint FirstPoint;
        public readonly IDataPoint SecondPoint;

        public SelectionMadeArgs(IDataPoint firstPoint, IDataPoint secondPoint)
        {
            FirstPoint = firstPoint;
            SecondPoint = secondPoint;
        }
    }

    public delegate void SelectionMade(object o, SelectionMadeArgs e);

    public delegate void SelectionReset(object o);
}
