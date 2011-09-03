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
		private List<DataValue> _missingValues = new List<DataValue>();
		private List<DataValue> _selectedValues = new List<DataValue>();
    	private List<Sensor> _sensorList = new List<Sensor>();

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

		public string ZoomText
		{
			get { return ZoomLevel + "%"; }
		}

        public String SensorName
        {
			get { return SelectedSensor == null ? "" : SelectedSensor.Name; }
        }

        public List<DataValue> MissingValues
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
			get { return _sensorList; }
			set
			{
				_sensorList = value;
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
				MissingValues = _sensor.CurrentState.GetMissingTimes(15);
				NotifyOfPropertyChange(() => SensorName);
				NotifyOfPropertyChange(() => MissingCount);
			}
		}

    	public List<DataValue> SelectedValues
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
			foreach (DataValue item in e.RemovedItems)
			{
				SelectedValues.Remove(item);
			}

			foreach (DataValue item in e.AddedItems)
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

            if(_selectedValues.Count == 0)
                return;

			foreach (DataValue dataValue in SelectedValues)
        	{
				DataValue prevValue = null;
				var time = 15;

				while (prevValue == null)
				{
					prevValue = _sensor.CurrentState.Values.Find(dv => dv.Timestamp.AddMinutes(time) == dataValue.Timestamp);
					time += 15;
				}
				var newDV = new DataValue(dataValue.Timestamp, 0);
				_sensor.CurrentState.Values.Insert(_sensor.CurrentState.Values.FindIndex(delegate(DataValue dv)
				{ return dv == prevValue; }) + 1, newDV);
        	}
            
            _missingValues = _sensor.CurrentState.GetMissingTimes(15);
            NotifyOfPropertyChange(()=>MissingValues);
			NotifyOfPropertyChange(() => SelectedSensor);
            NotifyOfPropertyChange(()=>MissingCount);

			Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);
        }

        public void btnSpecify()
        {
			//TODO refactor

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
                	bool exit = Common.ShowMessageBox("An Error Occured", "Please enter a valid number.", true, true);
					if (exit) return;
                }
            }

			foreach (DataValue dataValue in SelectedValues)
			{
				DataValue prevValue = null;
				var time = 15;

				while (prevValue == null)
				{
					prevValue = _sensor.CurrentState.Values.Find(dv => dv.Timestamp.AddMinutes(time) == dataValue.Timestamp);
					time += 15;
				}

				var newDV = new DataValue(dataValue.Timestamp, value);
				_sensor.CurrentState.Values.Insert(_sensor.CurrentState.Values.FindIndex(delegate(DataValue dv)
				                                                                         	{ return dv == prevValue; }) + 1, newDV);
			}

        	_missingValues = _sensor.CurrentState.GetMissingTimes(15);
            NotifyOfPropertyChange(() => MissingValues);
			NotifyOfPropertyChange(() => SelectedSensor);
            NotifyOfPropertyChange(() => MissingCount);

        	Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);
        }

		public void btnExtrapolate()
		{
			//TODO: Implement
			Common.ShowFeatureNotImplementedMessageBox();
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
