using System;
using System.Windows;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    class GraphBehaviour : BehaviourBase
    {
        public GraphBehaviour() : base("Custom Graph Behaviour")
        {
        }

        protected override void Init()
        {
            
        }

        public override void DeInit()
        {
            
        }

        public override void MouseLeftButtonDown(Point position)
        {
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
        }
    }
}
