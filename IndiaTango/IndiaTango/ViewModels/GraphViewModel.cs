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
        private const int MaxPointCount = 15000;

        public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            var b = new BehaviourManager {AllowMultipleEnabled = true};
            var g = new GraphBehaviour() {IsEnabled = true};
            g.ZoomRequested += delegate(object o, ZoomRequestedArgs e)
                                   {
                                       var filteredPoints = _dataPoints.Select(dataPointSet => dataPointSet.Where(x => x.X >= (DateTime) e.FirstPoint.X && x.X >= (DateTime) e.SecondPoint.X)).ToList();
                                       SampleValues(MaxPointCount,filteredPoints);
                                   };
            g.ZoomResetRequested += delegate(object o)
                                        {
                                            SampleValues(MaxPointCount,_dataPoints);
                                        };
            b.Behaviours.Add(g);
            Behaviour = b;
        }

        private List<DataSeries<DateTime, float>> _chartSeries = new List<DataSeries<DateTime, float>>();
        public List<DataSeries<DateTime, float>> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }

        private string _chartTitle = String.Empty;
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(()=> ChartTitle); } }

        private string _yAxisTitle = String.Empty;
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        private DoubleRange _range = new DoubleRange(0,0);
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        private IBehaviour _behaviour = new BehaviourManager();
        public IBehaviour Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        private List<IEnumerable<DataPoint<DateTime, float>>> _dataPoints = new List<IEnumerable<DataPoint<DateTime, float>>>();
        private List<string> _seriesNames = new List<string>();
        public Sensor Sensor { set
        {
            _dataPoints.Add(from dataValue in value.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
            _seriesNames.Add(value.Name);
            ChartTitle = value.Name;
            YAxisTitle = value.Unit;
            SampleValues(MaxPointCount, _dataPoints);
            Range = new DoubleRange(MinimumY().Y - 10, MaximumY().Y * 2);
        }}

        public List<Sensor> Sensors { set
        {
            ChartTitle = value[0].Name;
            for(int i = 0; i < value.Count(); i++)
            {
                _dataPoints.Add(from dataValue in value[i].CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
                _seriesNames.Add(value[i].Name);

                if (i > 0)
                    ChartTitle += " vs " + value[i].Name;
            }
            YAxisTitle = (((from dataSeries in value select dataSeries.Unit).Distinct()).Count() == 1) ? value[0].Unit : String.Empty;
            SampleValues(MaxPointCount, _dataPoints);
            Range = new DoubleRange(MinimumY().Y - 10, MaximumY().Y * 2);
        }}

        private DataPoint<DateTime, float> MaximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in series)
                {
                    if (maxY == null)
                        maxY = value;
                    else if (value.Y > maxY.Y)
                        maxY = value;
                }
            }
            if(maxY == null)
                return new DataPoint<DateTime, float>(DateTime.Now,10);
            return maxY;
        }

        private DataPoint<DateTime, float> MinimumY()
        {
            DataPoint<DateTime, float> minY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in series)
                {
                    if (minY == null)
                        minY = value;
                    else if (value.Y < minY.Y)
                        minY = value;
                }
            }
            if (minY == null)
                return new DataPoint<DateTime, float>(DateTime.Now, 0);
            System.Diagnostics.Debug.Print("Lowest Y value {0}", minY.Y);
            return minY;
        }

        private void SampleValues(int numberOfPoints, List<IEnumerable<DataPoint<DateTime, float>>> dataSource)
        {
            List<DataSeries<DateTime, float>> generatedSeries = new List<DataSeries<DateTime, float>>();
            for (int i = 0; i < dataSource.Count; i++)
            {
                var sampleRate = dataSource[i].Count() / (numberOfPoints / dataSource.Count());
                System.Diagnostics.Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", dataSource[i].Count(), numberOfPoints, sampleRate);
                var series = (sampleRate > 0) ? new DataSeries<DateTime, float>(_seriesNames[i], dataSource[i].Where((x, index) => index % sampleRate == 0)) : new DataSeries<DateTime, float>(_seriesNames[i], dataSource[i]);
                generatedSeries.Add(series);
            }
            ChartSeries = generatedSeries;
        }

        private void CountValues()
        {
            System.Diagnostics.Debug.Print("There are {0} data points", ChartSeries.Count);
        }
    }
}
