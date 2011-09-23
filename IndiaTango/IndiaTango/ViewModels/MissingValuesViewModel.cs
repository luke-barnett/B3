using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    public class MissingValuesViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private Sensor _sensor;
    	private int _zoomLevel = 100;
		private List<DateTime> _missingValues = new List<DateTime>();
		private List<DateTime> _selectedValues = new List<DateTime>();
        private Dataset _ds;

        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private BehaviourManager _behaviour;
        private DoubleRange _range;
        private int _sampleRate;
        private GraphableSensor _graphableSensor;
        private Canvas _backgroundCanvas;

        public MissingValuesViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;

            _backgroundCanvas = new Canvas { Visibility = Visibility.Collapsed };

            _behaviour = new BehaviourManager{AllowMultipleEnabled = true};

            var backgroundBehaviour = new GraphBackgroundBehaviour(_backgroundCanvas) { IsEnabled = true };

            _behaviour.Behaviours.Add(backgroundBehaviour);

            var zoomBehaviour = new CustomZoomBehaviour{ IsEnabled = true};
            zoomBehaviour.ZoomRequested += (o, e) =>
            {
                var startTime = (DateTime)e.FirstPoint.X;
                var endTime = (DateTime)e.SecondPoint.X;
                _graphableSensor.SetUpperAndLowerBounds(startTime, endTime);
                UpdateGraph();
            };

            zoomBehaviour.ZoomResetRequested += o =>
                                                    {
                                                        _graphableSensor.RemoveBounds();
                                                        UpdateGraph();
                                                    };

            _behaviour.Behaviours.Add(zoomBehaviour);

            Behaviour = _behaviour;
        }

		#region View Properties

        public bool RedoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.RedoStack.Count > 0; }
        }

        public bool UndoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.UndoStack.Count > 1; }
        }

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

        public Dataset Dataset { get { return _ds; } set { _ds = value; } }

		public string ZoomText
		{
			get { return ZoomLevel + "%"; }
		}

        public String SensorName
        {
			get { return SelectedSensor == null ? "" : SelectedSensor.Name; }
        }

        public List<DateTime> MissingValues
        {
            get { return _missingValues; }
			set
			{
				_missingValues = value;
				NotifyOfPropertyChange(() => MissingValues);
			}
        }

        public int MissingCount
        {
            get { return _missingValues.Count; }
            set { var i = value; }
        }

		public List<Sensor> SensorList
		{
			get { return _ds.Sensors; }
			set
			{
				_ds.Sensors = value;
				NotifyOfPropertyChange(() => SensorList);
			}
		}

		public Sensor SelectedSensor
		{
			get { return _sensor; }
			set
			{
				_sensor = value;
				NotifyOfPropertyChange(() => SelectedSensor);
				MissingValues = _sensor.CurrentState.GetMissingTimes(15,_ds.StartTimeStamp,_ds.EndTimeStamp);
				NotifyOfPropertyChange(() => SensorName);
				NotifyOfPropertyChange(() => MissingCount);
                NotifyOfPropertyChange(() => RedoButtonEnabled);
                NotifyOfPropertyChange(() => UndoButtonEnabled);
			    _graphableSensor = _sensor != null ? new GraphableSensor(_sensor) : null;
			    UpdateGraph();
			}
		}

    	public List<DateTime> SelectedValues
    	{
    		get { return _selectedValues; } 
			set
			{
				_selectedValues = value;
				NotifyOfPropertyChange(() => SelectedValues);
			}
    	}

        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }

        public BehaviourManager Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        public string ChartTitle { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Name; } }

        public string YAxisTitle { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Unit; } }

        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

		#endregion

		#region Event Handlers

		public void SelectionChanged(SelectionChangedEventArgs e)
		{
			foreach (DateTime item in e.RemovedItems)
			{
				SelectedValues.Remove(item);
			}

			foreach (DateTime item in e.AddedItems)
			{
				SelectedValues.Add(item);
			}
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            
		}

		public void btnUndo()
		{
			_sensor.Undo();
			MissingValues = _sensor.CurrentState.GetMissingTimes(_ds.DataInterval, _ds.StartTimeStamp, _ds.EndTimeStamp);
			RefreshGraph();
			NotifyOfPropertyChange(() => UndoButtonEnabled);
			NotifyOfPropertyChange(() => RedoButtonEnabled);
		}

		public void btnRedo()
		{
			_sensor.Redo();
			MissingValues = _sensor.CurrentState.GetMissingTimes(_ds.DataInterval, _ds.StartTimeStamp, _ds.EndTimeStamp);
			RefreshGraph();
			NotifyOfPropertyChange(() => UndoButtonEnabled);
			NotifyOfPropertyChange(() => RedoButtonEnabled);
		}


		public void btnDone()
		{
			this.TryClose();
		}

        public void btnMakeZero()
        {
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

            if(_selectedValues.Count == 0)
                return;

            _sensor.AddState(_sensor.CurrentState.MakeZero(SelectedValues));

            Finalise("Selected values set to 0.");

			Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);
            RefreshGraph();
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        }

        

        public void btnSpecify()
        {
			//TODO refactor
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

			if (_selectedValues.Count == 0)
				return;

            var value = float.MinValue;
            
			while (value.Equals(float.MinValue))
            {
                try
                {
                    var specifyVal = _container.GetInstance(typeof (SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
                    _windowManager.ShowDialog(specifyVal);
                    //cancel
                    if (specifyVal.Text == null)
                        return;
                    value = float.Parse(specifyVal.Text);
                }
                catch (FormatException f)
                {
                	var exit = Common.ShowMessageBox("An Error Occured", "Please enter a valid number.", true, true);
					if (exit) return;
                }
            }

            _sensor.AddState(_sensor.CurrentState.MakeValue(SelectedValues, value));

            Finalise("Selected values has been set to " + value + ".");

            Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);
            RefreshGraph();
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        }

        private void Finalise(string taskPerformed)
        {
            _missingValues = _sensor.CurrentState.GetMissingTimes(_ds.DataInterval, _ds.StartTimeStamp, _ds.EndTimeStamp);
            NotifyOfPropertyChange(() => MissingValues);
            NotifyOfPropertyChange(() => MissingCount);

            Common.requestReason(_sensor, _container, _windowManager, _sensor.CurrentState, taskPerformed);
        }

        public void btnExtrapolate()
        {
            EventLogger.LogInfo(GetType().ToString(), "Value extrapolation invoked.");

            if (SelectedSensor == null)
            {
                Common.ShowMessageBox("No sensor selected", "Please choose a sensor before performing extrapolation.",
                                      false, true);
                return;
            }

            if(SelectedValues.Count == 0)
            {
                Common.ShowMessageBox("No values selected", "Please select one or more data points before performing extrapolation.",
                                      false, true);
                return;
            }

            if (!Common.ShowMessageBox("Extrapolate", "This will find the first and last value in the current range and extrapolate between them.\r\n\r\nAre you sure you want to do this?", true, false))
                return;

            try
            {
                var newState = SelectedSensor.CurrentState.Extrapolate(SelectedValues, Dataset);
                SelectedSensor.AddState(newState);

                Finalise("Value extrapolation performed.");

                Common.ShowMessageBox("Values updated", "The values have been extrapolated successfully.", false, false);
            }
            catch (Exception e)
            {
                Common.ShowMessageBoxWithException("Error", "An error occured during extrapolation. Ensure you have selected a sensor and one or more data points, and try again.",
                                      false, true, e);
            }
            RefreshGraph();
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        }

		public void btnZoomIn()
		{
			//TODO: Implement zoom
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

        private void RefreshGraph()
        {
            if (_graphableSensor != null && _graphableSensor.DataPoints != null)
                _graphableSensor.RefreshDataPoints();
            UpdateGraph();
        }

        private void UpdateGraph()
        {
            if(SelectedSensor != null)
                SampleValues(Common.MaximumGraphablePoints, new Collection<GraphableSensor>{_graphableSensor});
            else
                ChartSeries =  new List<LineSeries>();
            NotifyOfPropertyChange(() => ChartTitle);
            NotifyOfPropertyChange(() => YAxisTitle);
        }

        private void SampleValues(int numberOfPoints, ICollection<GraphableSensor> sensors)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count() / (numberOfPoints / sensors.Count);
                Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
                if (_sampleRate > 1) ShowBackground();
            }

            ChartSeries = generatedSeries;
        }

        private void HideBackground()
        {
            _backgroundCanvas.Visibility = Visibility.Collapsed;
        }

        private void ShowBackground()
        {
            _backgroundCanvas.Visibility = Visibility.Visible;
        }
    }
}
