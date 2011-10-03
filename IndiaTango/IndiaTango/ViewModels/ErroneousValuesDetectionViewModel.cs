using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class ErroneousValuesDetectionViewModel : BaseViewModel
    {
        public ErroneousValuesDetectionViewModel(WindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            DetectionMethods = new List<IDetectionMethod> { new MissingValuesDetector(), new MissingValuesDetector(), new MissingValuesDetector(), new MissingValuesDetector() };
        }

        #region Private Variables

        private readonly WindowManager _windowManager;
        private readonly SimpleContainer _container;

        private Cursor _cursor = Cursors.Arrow;
        private List<IDetectionMethod> _detectionMethods = new List<IDetectionMethod>();
        private List<GraphableSensor> _sensorList = new List<GraphableSensor>();
        private List<String> _missingValues = new List<string>();
        private bool _showUndoStates;
        private bool _showRedoStates;
        private bool _undoButtonEnabled;
        private bool _redoButtonEnabled;
        private bool _actionButtonsEnabled;

        private Dataset _dataset;
        private GraphableSensor _selectedSensor;
        private Sensor Sensor { get { return (_selectedSensor == null) ? null : _selectedSensor.Sensor; } }

        #endregion

        #region Constants
        public string ViewTitle { get { return "Erroneous Values Detection"; } }
        #endregion

        #region Visual Items

        public Cursor ViewCursor { get { return _cursor; } set { _cursor = value; NotifyOfPropertyChange(() => ViewCursor); } }

        public bool ActionButtonsEnabled { get { return _actionButtonsEnabled; } set { _actionButtonsEnabled = value; NotifyOfPropertyChange(() => ActionButtonsEnabled); } }

        #endregion

        /// <summary>
        /// The DataSet to use
        /// </summary>
        public Dataset DataSet
        {
            set
            {
                _dataset = value;
                SensorList = (from sensor in _dataset.Sensors select new GraphableSensor(sensor)).ToList();
            }
        }

        #region Public Lists

        public List<IDetectionMethod> DetectionMethods { get { return _detectionMethods; } set { _detectionMethods = value; NotifyOfPropertyChange(() => DetectionMethods); } }

        public List<GraphableSensor> SensorList { get { return _sensorList; } set { _sensorList = value; NotifyOfPropertyChange(() => SensorList); } }

        public List<String> MissingValues { get { return _missingValues; } set { _missingValues = value; NotifyOfPropertyChange(() => MissingValues); } }

        #endregion

        #region Undo/Redo

        #region Undo

        public bool UndoButtonEnabled { get { return _undoButtonEnabled; } set { _undoButtonEnabled = value; NotifyOfPropertyChange(() => UndoButtonEnabled); } }

        public bool ShowUndoStates { get { return _showUndoStates; } set { _showUndoStates = value; NotifyOfPropertyChange(() => ShowUndoStates); } }

        public ReadOnlyCollection<SensorStateListObject> UndoStates
        {
            get
            {
                var ss = new List<SensorStateListObject>();

                var atStart = true;

                foreach (var obj in Sensor.UndoStates)
                {
                    if (atStart)
                    {
                        atStart = false;
                        continue;
                    }

                    ss.Add(new SensorStateListObject(obj, false));
                }

                ss.Add(new SensorStateListObject(Sensor.RawData, true));

                return new ReadOnlyCollection<SensorStateListObject>(ss);
            }
        }

        #endregion

        #region Redo

        public bool RedoButtonEnabled { get { return _redoButtonEnabled; } set { _redoButtonEnabled = value; NotifyOfPropertyChange(() => RedoButtonEnabled); } }

        public bool ShowRedoStates { get { return _showRedoStates; } set { _showRedoStates = value; NotifyOfPropertyChange(() => ShowRedoStates); } }

        public ReadOnlyCollection<SensorStateListObject> RedoStates
        {
            get
            {
                var ss = Sensor.RedoStates.Select(obj => new SensorStateListObject(obj, false)).ToList();

                return new ReadOnlyCollection<SensorStateListObject>(ss);
            }
        }

        #endregion

        #endregion

        #region Event Handlers

        #region Dectecion Methods

        public void DetectionMethodChecked(string name)
        {
            
        }

        public void DetectionMethodUnChecked(string name)
        {
            
        }

        #endregion

        #region Undo

        public void BtnUndo()
        {

        }

        public void UndoPathSelected()
        {

        }

        #endregion

        #region Redo

        public void BtnRedo()
        {

        }

        public void RedoPathSelected()
        {

        }

        #endregion

        public void BtnDone()
        {
            TryClose();
        }

        #region Value Changers

        public void BtnExtrapolate()
        {

        }

        public void BtnMakeZero()
        {

        }

        public void BtnSpecify()
        {

        }

        #endregion

        #endregion
    }
}
