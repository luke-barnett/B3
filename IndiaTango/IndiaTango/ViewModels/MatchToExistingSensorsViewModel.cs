using System.Collections.Generic;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class MatchToExistingSensorsViewModel : BaseViewModel
    {
        #region Private Variables

        private List<Sensor> _existingSensors = new List<Sensor>();
        private List<Sensor> _newSensors = new List<Sensor>();
        private List<SensorMatch> _matches = new List<SensorMatch>();
        private Sensor _selectedNewSensor;
        private Sensor _selectedExistingSensor;
        private SensorMatch _selectedSensorMatch;

        #endregion

        #region Public Parameters

        public List<Sensor> ExistingSensors
        {
            get { return _existingSensors; }
            set
            {
                _existingSensors = value;
                NotifyOfPropertyChange(() => ExistingSensors);
            }
        }

        public List<Sensor> NewSensors
        {
            get { return _newSensors; }
            set
            {
                _newSensors = value;
                NotifyOfPropertyChange(() => NewSensors);
            }
        }

        public List<SensorMatch> SensorLinks
        {
            get { return _matches; }
            set
            {
                _matches = value;
                NotifyOfPropertyChange(() => SensorLinks);
            }
        }

        public Sensor SelectedNewSensor
        {
            get { return _selectedNewSensor; }
            set
            {
                _selectedNewSensor = value;
                NotifyOfPropertyChange(() => SelectedNewSensor);
            }
        }

        public Sensor SelectedExistingSensor
        {
            get { return _selectedExistingSensor; }
            set
            {
                _selectedExistingSensor = value;
                NotifyOfPropertyChange(() => SelectedExistingSensor);
            }
        }

        public SensorMatch SelectedSensorMatch
        {
            get { return _selectedSensorMatch; }
            set
            {
                _selectedSensorMatch = value;
                NotifyOfPropertyChange(() => SelectedSensorMatch);
            }
        }

        #endregion

        #region Public Methods

        public void MakeLink()
        {

        }

        public void RemoveLink()
        {

        }

        public void Done()
        {
            TryClose();
        }

        #endregion
    }

    public class SensorMatch
    {
        public readonly Sensor ExistingSensor;
        public readonly Sensor MatchingSensor;

        public SensorMatch(Sensor existingSensor, Sensor matchingSensor)
        {
            ExistingSensor = existingSensor;
            MatchingSensor = matchingSensor;
        }

        public override string ToString()
        {
            return string.Format("{0} --> {1}", MatchingSensor, ExistingSensor);
        }
    }
}
