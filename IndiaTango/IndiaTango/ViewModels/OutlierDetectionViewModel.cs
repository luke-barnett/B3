using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    internal class OutlierDetectionViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private List<DateTime> _selectedValues = new List<DateTime>();
        private List<DateTime> _outliers = new List<DateTime>();
        private Dataset _ds;
        private int _zoomLevel = 100;
        private Sensor _sensor;

        public OutlierDetectionViewModel(IWindowManager manager, SimpleContainer container)
        {
            _windowManager = manager;
            _container = container;
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

        public Dataset Dataset
        {
            get { return _ds; }
            set { _ds = value; }
        }

        public string ZoomText
        {
            get { return ZoomLevel + "%"; }
        }

        public String SensorName
        {
            get { return SelectedSensor == null ? "" : SelectedSensor.Name; }
        }

        public List<string> OutliersStrings
        {
            get
            {
                var list = new List<String>();
                foreach (var time in _outliers)
                {
                    list.Add(time.ToShortDateString().PadRight(12)+time.ToShortTimeString().PadRight(15) + _sensor.CurrentState.Values[time]);
                }
                return list;
            }
        }

        public List<DateTime> Outliers
        {
            get { return _outliers; }
            set
            {
                _outliers = value;
                NotifyOfPropertyChange(() => Outliers);
            }
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
                Outliers = _sensor.CurrentState.GetOutliers(_ds.DataInterval, _ds.StartTimeStamp, _ds.EndTimeStamp,
                                                            _sensor.UpperLimit, _sensor.LowerLimit,
                                                            _sensor.MaxRateOfChange);
                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => OutliersStrings);
            }
        }

        public List<DateTime> SelectedValues
        {
            get { return _selectedValues; }
            set
            {
                if(value!=null)
                    _selectedValues = value;
                else
                    _selectedValues = new List<DateTime>();
                NotifyOfPropertyChange(() => SelectedValues);
            }
        }




        #endregion

        #region button event handlers

        public void SelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (string item in e.RemovedItems)
            {
                SelectedValues.Remove(DateTime.Parse(item.Substring(0, 27)));
            }

            foreach (string item in e.AddedItems)
            {
                SelectedValues.Add(DateTime.Parse(item.Substring(0, 27)));
            }
        }


        public void btnRemove()
        {
            EventLogger.LogInfo(GetType().ToString(), "Value removal started.");
            if (_selectedValues.Count == 0)
                return;
            _sensor.AddState(_sensor.CurrentState.removeValues(SelectedValues));

            Finalise();
            Common.ShowMessageBox("Values Updated", "The selected values have been removed from the data", false, false);
            EventLogger.LogInfo(GetType().ToString(), "Value removal complete. Sensor: " + SelectedSensor.Name);
        }

        public void btnMakeZero()
        {
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

            if (_selectedValues.Count == 0)
                return;

            _sensor.AddState(_sensor.CurrentState.ChangeToZero(SelectedValues));

            Finalise();

            Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);
            EventLogger.LogInfo(GetType().ToString(), "Value updation complete. Sensor: " + SelectedSensor.Name + ". Value: 0.");
        }

        private void Finalise()
        {
            _outliers = _sensor.CurrentState.GetOutliers(_ds.DataInterval, _ds.StartTimeStamp, _ds.EndTimeStamp,
                                                         _sensor.UpperLimit, _sensor.LowerLimit, _sensor.MaxRateOfChange);
            NotifyOfPropertyChange(() => Outliers);
            NotifyOfPropertyChange(() => OutliersStrings);

            requestReason();
        }


        public void requestReason()
        {
            if (_sensor != null && _sensor.CurrentState != null)
            {
                var specify = (SpecifyValueViewModel)_container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel");
                specify.Title = "Log Reason";
                specify.Message = "Please specify a reason for this change:";
                specify.Deactivated += (o, e) =>
                {
                    // Specify reason
                    _sensor.CurrentState.Reason = specify.Text;
                };
                _windowManager.ShowDialog(specify);
            }
        }

        public void btnSpecify()
        {
            //TODO refactor
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

            if (_selectedValues.Count == 0)
                return;

            var value = float.MinValue;

            while (value == float.MinValue)
            {
                try
                {
                    var specifyVal = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
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

            _sensor.AddState(_sensor.CurrentState.ChangeToValue(SelectedValues, value));

            Finalise();

            Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);
            EventLogger.LogInfo(GetType().ToString(), "Value updation complete. Sensor: " + SelectedSensor.Name + ". Value: " + value + ".");
        }
        #endregion

    }
}
