using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public List<String> OutliersStrings
        {
            get
            {
                var list = new List<String>();
                foreach (var time in _outliers)
                {
                    list.Add(time.ToShortDateString()+" "+time.ToShortTimeString().PadRight(10) + _sensor.CurrentState.Values[time]);
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
                _selectedValues = value;
                NotifyOfPropertyChange(() => SelectedValues);
            }
        }


        #endregion

    }
}
