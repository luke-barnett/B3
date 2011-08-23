using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;

namespace IndiaTango.ViewModels
{
    class GraphViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private const int MaxPointCount = 5000;

        public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            var b = new BehaviourManager {AllowMultipleEnabled = true};
            var g = new GraphBehaviour() {IsEnabled = true};
            g.ZoomRequested += delegate(object o, ZoomRequestedArgs e)
                                   {
                                       SampleValues(MaxPointCount, _dataPoints.Where(x => x.X >= (DateTime)e.FirstPoint.X && x.X >= (DateTime)e.SecondPoint.X));
                                   };
            g.ZoomResetRequested += delegate(object o)
                                        {
                                            SampleValues(MaxPointCount,_dataPoints);
                                        };
            b.Behaviours.Add(g);
            Behaviour = b;
        }
        
        private DataSeries<DateTime, float> _chartSeries = new DataSeries<DateTime, float>();
        public DataSeries<DateTime, float> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); CountValues(); } }

        private string _chartTitle = String.Empty;
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(()=> ChartTitle); } }

        private string _yAxisTitle = String.Empty;
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        private DoubleRange _range = new DoubleRange(0,0);
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        private IBehaviour _behaviour = new BehaviourManager();
        public IBehaviour Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        private IEnumerable<DataPoint<DateTime, float>> _dataPoints;
        public Sensor Sensor { set
        {
            _dataPoints = (from dataValue in value.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
            ChartTitle = value.Name;
            YAxisTitle = value.Unit;
            SampleValues(MaxPointCount, _dataPoints);
            Range = new DoubleRange(0, maximumY().Y * 2);
        }}

        private DataPoint<DateTime, float> maximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var value in ChartSeries)
            {
                if (maxY == null)
                    maxY = value;
                else if (value.Y > maxY.Y)
                    maxY = value;
            }
            if(maxY == null)
                return new DataPoint<DateTime, float>(DateTime.Now,10);
            return maxY;
        }

        private void SampleValues(int numberOfPoints, IEnumerable<DataPoint<DateTime, float>> dataSource)
        {
            if (numberOfPoints < 2)
                return;
            var sampleRate = _dataPoints.Count() / numberOfPoints;
            System.Diagnostics.Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}",_dataPoints.Count(),numberOfPoints,sampleRate);
            var series = (sampleRate > 0) ? new DataSeries<DateTime, float>(ChartTitle, dataSource.Where((x, index) => index % sampleRate == 0)) : ChartSeries = new DataSeries<DateTime, float>(ChartTitle, dataSource);
            if (series.Count > 1)
                ChartSeries = series;
        }

        private void CountValues()
        {
            System.Diagnostics.Debug.Print("There are {0} data points", ChartSeries.Count);
        }
    }
}
