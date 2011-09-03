using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    class GraphViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private readonly GraphBehaviour _graphBehaviour;
        private readonly Canvas _graphBackground;
        private const int MaxPointCount = 15000;
		private List<Sensor> _sensorList = new List<Sensor>();
		private List<Sensor> _renderedSensors = new List<Sensor>();
		private List<Sensor> _checkedSensors = new List<Sensor>();
    	private bool _columnVisible = true;
    	private int _zoomLevel = 100;

		public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
		{
			_windowManager = windowManager;
			_container = container;


			_graphBackground = new Canvas();
			var b = new BehaviourManager { AllowMultipleEnabled = true };
			_graphBehaviour = new GraphBehaviour(_graphBackground) { IsEnabled = true };
			_graphBehaviour.ZoomRequested += (o, e) =>
			{
				var filteredPoints =
					_dataPoints.Select(
						dataPointSet =>
						dataPointSet.Where(
							x =>
							x.X >= (DateTime)e.FirstPoint.X && x.X >= (DateTime)e.SecondPoint.X))
						.ToList();
				SampleValues(MaxPointCount, filteredPoints);
			};
			_graphBehaviour.ZoomResetRequested += o => SampleValues(MaxPointCount, _dataPoints);
			b.Behaviours.Add(_graphBehaviour);
			Behaviour = b;
		}

		#region Graph Properties
        

        private List<LineSeries> _chartSeries = new List<LineSeries>();
        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }

        private string _chartTitle = String.Empty;
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(()=> ChartTitle); } }

        private string _yAxisTitle = String.Empty;
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        private DoubleRange _range = new DoubleRange(0,0);
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        private IBehaviour _behaviour = new BehaviourManager();
        public IBehaviour Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        private double _minimum = 0;
        public double Minimum { get { return _minimum; } set { _minimum = value; NotifyOfPropertyChange(() => Minimum); NotifyOfPropertyChange(() => MinimumValue); MinimumMaximum = Minimum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1; } }

        public string MinimumValue { get { return string.Format("Y Axis Min: {0}", (int)Minimum); } }

        private double _maximumMinimum;
        public double MaximumMinimum { get { return _maximumMinimum; } set { _maximumMinimum = value; NotifyOfPropertyChange(() => MaximumMinimum); } }

        private double _minimumMinimum;
        public double MinimumMinimum { get { return _minimumMinimum; } set { _minimumMinimum = value; NotifyOfPropertyChange(() => MinimumMinimum); } }

        private double _maximum = 0;
        public double Maximum { get { return _maximum; } set { _maximum = value; NotifyOfPropertyChange(() => Maximum); NotifyOfPropertyChange(() => MaximumValue); MaximumMinimum = Maximum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1; } }

        public string MaximumValue { get { return string.Format("Y Axis Max: {0}", (int)Maximum); } }

        private double _maximumMaximum;
        public double MaximumMaximum { get { return _maximumMaximum; } set { _maximumMaximum = value; NotifyOfPropertyChange(() => MaximumMaximum); } }

        private double _minimumMaximum;
        public double MinimumMaximum { get { return _minimumMaximum; } set { _minimumMaximum = value; NotifyOfPropertyChange(() => MinimumMaximum); } }

        private bool _sampledValues;
        public bool SampledValues { get { return _sampledValues; } set { _sampledValues = value; NotifyOfPropertyChange(() => SampledValuesString); } }
        public string SampledValuesString { get { return (SampledValues) ? "Sampling every " + sampleRate + " values" : String.Empty; } }

        private List<IEnumerable<DataPoint<DateTime, float>>> _dataPoints = new List<IEnumerable<DataPoint<DateTime, float>>>();
        private List<string> _seriesNames = new List<string>();

        public List<Sensor> RenderedSensors 
		{ 
			set
			{_renderedSensors = value;
				for(var i = 0; i < value.Count(); i++)
				{
					
				}

				
			}
			get { return _renderedSensors; }
		}

    	public List<Sensor> SensorList
    	{
			get { return _sensorList;  }
			set
			{
				_sensorList = value;
				NotifyOfPropertyChange(() => SensorList);
			}
    	}

    	public Sensor SelectedSensor = null;

        private DataPoint<DateTime, float> MaximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in (DataSeries<DateTime, float>)series.DataSeries)
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
                foreach (var value in (DataSeries<DateTime, float>)series.DataSeries)
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

        private int sampleRate = 0;

        private void SampleValues(int numberOfPoints, IList<IEnumerable<DataPoint<DateTime, float>>> dataSource)
        {
            var generatedSeries = new List<LineSeries>();
            HideBackground();
            for (var i = 0; i < dataSource.Count; i++)
            {
                sampleRate = dataSource[i].Count() / (numberOfPoints / dataSource.Count());
                System.Diagnostics.Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", dataSource[i].Count(), numberOfPoints, sampleRate);

				//TODO: Index out of range exception of sensor removal
                
				var series = (sampleRate > 1) ? new DataSeries<DateTime, float>(_seriesNames[i], dataSource[i].Where((x, index) => index % sampleRate == 0)) : new DataSeries<DateTime, float>(_seriesNames[i], dataSource[i]);
                generatedSeries.Add(new LineSeries{ DataSeries = series/*, LineStroke = Brushes.Black*/}); //This is where we have a list of brushes to wrap
                if (sampleRate > 1) ShowBackground();
            }
            ChartSeries = generatedSeries;
            _graphBehaviour.RefreshVisual();
        }

        private void ShowBackground()
        {
            _graphBackground.Visibility = Visibility.Visible;
            SampledValues = true;
        }

        private void HideBackground()
        {
            _graphBackground.Visibility = Visibility.Collapsed;
            SampledValues = false;
        }
		
		#endregion

		#region View Properties

    	public int ZoomLevel
    	{
			get { return _zoomLevel; }
			set
			{
				_zoomLevel = Math.Max(100, value);
				_zoomLevel = Math.Min(1000, _zoomLevel);

				NotifyOfPropertyChange(() => ZoomLevel);
				NotifyOfPropertyChange(() => ZoomText);

				//TODO: Actually zoom
			}
    	}

    	public string ZoomText
    	{
    		get { return ZoomLevel + "%"; }
    	}

		public int ColumnWidth
		{
			get { return _columnVisible ? 250 : 0; }
		}

    	public ImageSource ToggleButtonImage
    	{
			get 
			{
				return _columnVisible 
					? new BitmapImage(new Uri("pack://application:,,,/Images/expand_left.png")) 
					: new BitmapImage(new Uri("pack://application:,,,/Images/expand_right.png")); 
			}
    	}

		#endregion
		
		#region Graph Methods
		
		private void RemoveSensor(Sensor sensor)
		{
			if (!RenderedSensors.Contains(sensor))
				return;

			RenderedSensors.Remove(sensor);

			_dataPoints.Remove(from dataValue in sensor.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
			_seriesNames.Remove(sensor.Name);

			RedrawGraph();
		}

		private void AddSensor(Sensor sensor)
		{
			if(RenderedSensors.Contains(sensor))
				return;

			RenderedSensors.Add(sensor);

			_dataPoints.Add(from dataValue in sensor.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value));
			_seriesNames.Add(sensor.Name);

			RedrawGraph();
		}

		private void RedrawGraph()
		{
			foreach (Sensor sen in RenderedSensors)
			{
				ChartTitle += " VS " + sen.Name;
			}

			YAxisTitle = (((from dataSeries in RenderedSensors select dataSeries.Unit).Distinct()).Count() == 1) ? RenderedSensors[0].Unit : String.Empty;
			SampleValues(MaxPointCount, _dataPoints);

			MaximumMaximum = MaximumY().Y + 100;
			MinimumMinimum = MinimumY().Y - 100;

			Maximum = MaximumMaximum;
			Minimum = MinimumMinimum;
		}

		#endregion

		#region Event Handlers

		public void btnColumnToggle()
		{
			_columnVisible = !_columnVisible;
			NotifyOfPropertyChange(() => ColumnWidth);
			NotifyOfPropertyChange(() => ToggleButtonImage);
		}

		public void SelectionChanged(SelectionChangedEventArgs e)
		{
			SelectedSensor = (Sensor)e.AddedItems[0];
			//MessageBox.Show("selected");
		}

		public void SensorChecked(RoutedEventArgs e)
		{
			CheckBox check = (CheckBox)e.Source;
			Sensor sensor = (Sensor) check.Content;
			AddSensor(sensor);
		}

		public void SensorUnchecked(RoutedEventArgs e)
		{
			//TODO: Fix index out of range exception
			CheckBox check = (CheckBox)e.Source;
			check.IsChecked = true;
			//Sensor sensor = (Sensor)check.Content;
			//RemoveSensor(sensor);

		}

		public void btnZoomIn()
		{
			ZoomLevel += 100;
		}

		public void btnZoomOut()
		{
			ZoomLevel -= 100;
		}

		public void sldZoom(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ZoomLevel = (int)e.NewValue;
		}

		#endregion
    }
}
