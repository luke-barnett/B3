using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    internal class MissingValuesViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private Sensor _sensor;
        private List<DataValue> _missingValues;

        public MissingValuesViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;
        }

        public Sensor Sensor
        {
            get { return _sensor; }
            set
            {
                _sensor = value;
                NotifyOfPropertyChange(() => Sensor);
                _missingValues = _sensor.CurrentState.GetMissingTimes(15);
                NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => MissingCount);
            }
        }

        public String SensorName
        {
            get { return Sensor.Name; }
        }

        public List<DataValue> MissingValues
        {
            get { return _missingValues; }
        }

        public int MissingCount
        {
            get { return _missingValues.Count; }
            set { var i = value; }
        }



    }
}
