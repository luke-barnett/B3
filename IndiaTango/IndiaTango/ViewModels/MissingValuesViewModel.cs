using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    internal class MissingValuesViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private Sensor _sensor;
    	private int _zoomLevel = 100;
		private List<DateTime> _missingValues = new List<DateTime>();
		private List<DateTime> _selectedValues = new List<DateTime>();
        private Dataset _ds;

        public MissingValuesViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;
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
		}

		public void btnUndo()
		{
            _sensor.Undo();
            MissingValues = _sensor.CurrentState.GetMissingTimes(15, _ds.StartTimeStamp, _ds.EndTimeStamp);
            
		}

		public void btnRedo()
		{
            _sensor.Redo();
            MissingValues = _sensor.CurrentState.GetMissingTimes(15, _ds.StartTimeStamp, _ds.EndTimeStamp);
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
        }

        

        public void btnSpecify()
        {
			//TODO refactor
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

			if (_selectedValues.Count == 0)
				return;

            var value = Int32.MinValue;
            
			while (value == Int32.MinValue)
            {
                try
                {
                    var specifyVal = _container.GetInstance(typeof (SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
                    _windowManager.ShowDialog(specifyVal);
                    //cancel
                    if (specifyVal.Text == null)
                        return;
                    value = Int32.Parse(specifyVal.Text);
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
    }
}
