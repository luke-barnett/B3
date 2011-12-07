using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private bool _runSensorMatch;

        #endregion

        #region Public Parameters

        public string Title { get { return "Sensor Matching"; } }

        public List<Sensor> ExistingSensors
        {
            get { return _existingSensors; }
            set
            {
                _existingSensors = new List<Sensor>(value);
                SensorMatch();
                NotifyOfPropertyChange(() => ExistingSensors);
            }
        }

        public List<Sensor> NewSensors
        {
            get { return _newSensors; }
            set
            {
                _newSensors = new List<Sensor>(value);
                SensorMatch();
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
                Debug.Print("New Selected New Sensor {0}", SelectedNewSensor);
                NotifyOfPropertyChange(() => SelectedNewSensor);
            }
        }

        public Sensor SelectedExistingSensor
        {
            get { return _selectedExistingSensor; }
            set
            {
                _selectedExistingSensor = value;
                Debug.Print("New Selected Existing Sensor {0}", SelectedExistingSensor);
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
            MakeLink(SelectedExistingSensor, SelectedNewSensor);
        }

        public void RemoveLink()
        {
            if (SelectedSensorMatch == null || !SensorLinks.Contains(SelectedSensorMatch))
                return;

            ExistingSensors.Add(SelectedSensorMatch.ExistingSensor);
            ExistingSensors = new List<Sensor>(ExistingSensors);

            NewSensors.Add(SelectedSensorMatch.MatchingSensor);
            NewSensors = new List<Sensor>(NewSensors);

            SensorLinks.Remove(SelectedSensorMatch);
            SensorLinks = new List<SensorMatch>(SensorLinks);
        }

        public void Done()
        {
            TryClose();
        }

        #endregion

        #region Private Methods

        private void MakeLink(Sensor existing, Sensor matching)
        {
            Debug.Print("Existing {0} Matching {1} Existing in list {2} Matching in list {3}", existing, matching, ExistingSensors.Contains(existing), NewSensors.Contains(matching));
            if (existing == null || matching == null || !ExistingSensors.Contains(existing) || !NewSensors.Contains(matching))
                return;

            SensorLinks.Add(new SensorMatch(existing, matching));
            SensorLinks = new List<SensorMatch>(SensorLinks);

            ExistingSensors.Remove(existing);
            ExistingSensors = new List<Sensor>(ExistingSensors);
            NewSensors.Remove(matching);
            NewSensors = new List<Sensor>(NewSensors);
        }

        private void SensorMatch()
        {
            if (_runSensorMatch || NewSensors.Count == 0 || ExistingSensors.Count == 0)
                return;
            _runSensorMatch = true;

            var matchesMade = new List<SensorMatch>();

            foreach (var newSensor in NewSensors)
            {
                var matchingExistingSensor = ExistingSensors.FirstOrDefault(x => x.Name == newSensor.Name);
                if (matchingExistingSensor == null)
                    continue;
                if (matchesMade.FirstOrDefault(x => x.ExistingSensor == matchingExistingSensor) == null)
                    matchesMade.Add(new SensorMatch(matchingExistingSensor, newSensor));
            }

            foreach (var sensorMatch in matchesMade)
            {
                ExistingSensors.Remove(sensorMatch.ExistingSensor);
                NewSensors.Remove(sensorMatch.MatchingSensor);
            }

            ExistingSensors = new List<Sensor>(ExistingSensors);
            NewSensors = new List<Sensor>(NewSensors);

            SensorLinks = new List<SensorMatch>(matchesMade);
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
