﻿using System;
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

        public CustomSelectionBehaviour()
            : base("Selection Behaviour")
        {
        }

        protected override void Init()
        {
            _selectionRectangle.Visibility = Visibility.Visible;
            BehaviourContainer.Children.Add(_selectionRectangle);
        }

        public override void DeInit()
        {
            if (BehaviourContainer != null && BehaviourContainer.Children.Contains(_selectionRectangle))
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
            if (Chart.XAxis.ActualRange == null || Chart.YAxis.ActualRange == null || !BehaviourContainer.CaptureMouse())
                return;

            _leftMouseDown = true;
            _firstPosition = position;

            //Set the up the zoom rectangle
            _selectionRectangle.SetValue(Canvas.LeftProperty, position.X);
            _selectionRectangle.SetValue(Canvas.TopProperty, position.Y);
            _selectionRectangle.Width = 0;
            _selectionRectangle.Height = 0;
            if (_selectionRectangle.Border != null)
            {
                _selectionRectangle.Border.Background = _selectionRectangle.Background;
                _selectionRectangle.Border.BorderBrush = _selectionRectangle.Foreground;
            }

            //Make it visible
            _selectionRectangle.Visibility = Visibility.Visible;
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

            if (Math.Abs(position.X - _firstPosition.X) < 2)
            {
                _selectionRectangle.Width = 0;
                _selectionRectangle.Height = 0;
            }

            MakeSelection(_firstPosition, position);
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
            }
            else
            {
                _selectionRectangle.Width = _firstPosition.X - position.X;
                _selectionRectangle.SetValue(Canvas.LeftProperty, position.X);
            }

            if (position.Y > _firstPosition.Y)
            {
                _selectionRectangle.Height = position.Y - _firstPosition.Y;
            }
            else
            {
                _selectionRectangle.Height = _firstPosition.Y - position.Y;
                _selectionRectangle.SetValue(Canvas.TopProperty, position.Y);
            }
        }

        private void MakeSelection(Point firstPoint, Point secondPoint)
        {
            var x1 = (DateTime)Chart.XAxis.GetRenderPositionAsDataValueWithoutZoom(firstPoint.X);
            var x2 = (DateTime)Chart.XAxis.GetRenderPositionAsDataValueWithoutZoom(secondPoint.X);
            var y1 = (Double)Chart.YAxis.GetRenderPositionAsDataValueWithoutZoom(firstPoint.Y);
            var y2 = (Double)Chart.YAxis.GetRenderPositionAsDataValueWithoutZoom(secondPoint.Y);

            if (SelectionMade != null)
                SelectionMade(this, new SelectionMadeArgs(x1, x2, (float)y1, (float)y2));
        }

    }

    public class SelectionMadeArgs : EventArgs
    {
        public readonly DateTime UpperX;
        public readonly DateTime LowerX;
        public readonly double UpperY;
        public readonly double LowerY;

        public SelectionMadeArgs(DateTime x1, DateTime x2, float y1, float y2)
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
            Debug.Print("New Selection Made: Date Range {0} - {1} , ValueRange {2} - {3}", LowerX, UpperX, LowerY, UpperY);
        }
    }

    public delegate void SelectionMade(object o, SelectionMadeArgs e);

    public delegate void SelectionReset(object o);
}
