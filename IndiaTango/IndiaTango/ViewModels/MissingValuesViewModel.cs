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
			//TODO actual code
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnRedo()
		{
			//TODO actual code
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnDone()
		{
			this.TryClose();
		}

        public void btnMakeZero()
        {
			//TODO refactor
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

            if(_selectedValues.Count == 0)
                return;

			foreach (DateTime time in SelectedValues)
        	{
				var prevValue = FindPrevValue(time);
        	    var newDV = new DataValue(time, 0);
				_sensor.CurrentState.Values.Insert(_sensor.CurrentState.Values.FindIndex(delegate(DataValue dv)
				{ return dv == prevValue; }) + 1, newDV);
        	}
            
            Cleanup();

			Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);
            EventLogger.LogInfo(GetType().ToString(), "Value updation complete. Sensor: " + SelectedSensor.Name + ". Value: 0.");
        }

        private DataValue FindPrevValue(DateTime dataValue)
        {
            DataValue prevValue = null;
            var time = 15;

            while (prevValue == null)
            {
                prevValue = _sensor.CurrentState.Values.Find(dv => dv.Timestamp.AddMinutes(time) == dataValue);
                time += 15;
            }
            return prevValue;
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

			foreach (var time in SelectedValues)
			{
                var prevValue = FindPrevValue(time);

				var newDV = new DataValue(time, value);
				_sensor.CurrentState.Values.Insert(_sensor.CurrentState.Values.FindIndex(delegate(DataValue dv)
				                                                                         	{ return dv == prevValue; }) + 1, newDV);
			}

        	Cleanup();

            Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);
            EventLogger.LogInfo(GetType().ToString(),"Value updation complete. Sensor: " + SelectedSensor.Name + ". Value: " + value + ".");

        }

        private void Cleanup()
        {
            _missingValues = _sensor.CurrentState.GetMissingTimes(15, _ds.StartTimeStamp, _ds.EndTimeStamp);
            NotifyOfPropertyChange(() => MissingValues);
            NotifyOfPropertyChange(() => MissingCount);
        }

        public void btnExtrapolate()
        {
            EventLogger.LogInfo(GetType().ToString(), "Value Extrapolation started.");
			//TODO: Refactor
            if (SelectedValues.Count == 0)
                return;
            if (!Common.ShowMessageBox("Extrapolate", "Will find first and last value in current range and extrapolate between them.\nPlease confirm:",true,false))
            {
                return;
            }
		    var first = SelectedValues[0];
            var startValue = FindPrevValue(first);
		    DataValue endValue = null;
		    var time = -15;
            while (endValue == null)
            {
                endValue = _sensor.CurrentState.Values.Find(dv => dv.Timestamp.AddMinutes(time) == first);
                time -= 15;
            }
            var tempList = new List<DataValue>();
		    var timeDiff = endValue.Timestamp.Subtract(startValue.Timestamp).TotalMinutes;
		    var valDiff = endValue.Value - startValue.Value;
		    var step = valDiff/(timeDiff/15);
		    var value = startValue.Value + step;
            var newDV = new DataValue(startValue.Timestamp.AddMinutes(15), (float)Math.Round(value,2));
            tempList.Add(newDV);
            for(var i = 30;i<timeDiff;i+=15)
            {
                newDV = new DataValue(startValue.Timestamp.AddMinutes(i), (float)Math.Round(value,2));
                tempList.Add(newDV);
                value += step;
            }
            _sensor.CurrentState.Values.InsertRange(_sensor.CurrentState.Values.FindIndex(delegate(DataValue dv)
                { return dv == startValue; })+1,tempList);
            Cleanup();

            Common.ShowMessageBox("Values Updated", "The vaues have been extrapolated", false, false);
            EventLogger.LogInfo(GetType().ToString(),
                                "Value extrapolation complete. Sensor: " + SelectedSensor.Name + ". Range: " +
                                startValue.Timestamp + " to " + endValue.Timestamp);
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
