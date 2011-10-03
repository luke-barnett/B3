using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private IDetectionMethod _selectedDetectionMethod;
        private Sensor Sensor { get { return (_selectedSensor == null) ? null : _selectedSensor.Sensor; } }
        private List<IDetectionMethod> _selectedMethods = new List<IDetectionMethod>();
        private List<string> _selectedMissingValues = new List<string>();
        private Visibility _detectionSettingsVisibility = Visibility.Collapsed;
        private Grid _detectionSettingsGrid;
        private GridLength _detectionSettingsHeight = new GridLength(0);

        #endregion

        #region Constants
        public string ViewTitle { get { return "Erroneous Values Detection"; } }
        #endregion

        #region Visual Items

        public Cursor ViewCursor { get { return _cursor; } set { _cursor = value; NotifyOfPropertyChange(() => ViewCursor); } }

        public bool ActionButtonsEnabled { get { return _actionButtonsEnabled; } set { _actionButtonsEnabled = value; NotifyOfPropertyChange(() => ActionButtonsEnabled); } }

        public Visibility DetectionSettingsVisibility { get { return _detectionSettingsVisibility; } set { _detectionSettingsVisibility = value; NotifyOfPropertyChange(() => DetectionSettingsVisibility); } }

        public Grid DetectionSettingsGrid { get { return _detectionSettingsGrid; } set { _detectionSettingsGrid = value; NotifyOfPropertyChange(() => DetectionSettingsGrid); } }

        public GridLength DetectionSettingsHeight { get { return _detectionSettingsHeight; } set { _detectionSettingsHeight = value; NotifyOfPropertyChange(() => DetectionSettingsHeight); } }

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

        public GraphableSensor SelectedSensor { get { return _selectedSensor; } set { _selectedSensor = value; NotifyOfPropertyChange(() => SelectedSensor); FindErroneousValues(); } }

        public IDetectionMethod SelectedDetectionMethod { get { return _selectedDetectionMethod; } set { _selectedDetectionMethod = value; NotifyOfPropertyChange(() => SelectedDetectionMethod); UpdateDetectionMethodsSettings(); } }

        #region Public Lists

        public List<IDetectionMethod> DetectionMethods { get { return _detectionMethods; } set { _detectionMethods = value; NotifyOfPropertyChange(() => DetectionMethods); } }

        public List<GraphableSensor> SensorList { get { return _sensorList; } set { _sensorList = value; NotifyOfPropertyChange(() => SensorList); } }

        public List<string> MissingValues { get { return _missingValues; } set { _missingValues = value; NotifyOfPropertyChange(() => MissingValues); } }

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

        #region Detection Methods

        public void DetectionMethodChecked(RoutedEventArgs eventArgs)
        {
            var checkBox = eventArgs.Source as CheckBox;
            if (checkBox == null) return;

            var method = (checkBox.Content as IDetectionMethod);
            Debug.WriteLine("Adding {0} to selected detection methods", method);
            _selectedMethods.Add(method);

            SelectedDetectionMethod = method;
        }

        public void DetectionMethodUnChecked(RoutedEventArgs eventArgs)
        {
            var checkBox = eventArgs.Source as CheckBox;
            if (checkBox == null) return;

            var method = (checkBox.Content as IDetectionMethod);
            Debug.WriteLine("Removing {0} from selected detection methods", method);
            if (_selectedMethods.Contains(method))
                _selectedMethods.Remove(method);

            SelectedDetectionMethod = method;
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

        public void MissingValuesSelectionChanged(SelectionChangedEventArgs e)
        {
            //Add all new values
            foreach (var addedItem in e.AddedItems)
            {
                _selectedMissingValues.Add((string)addedItem);
            }

            //Remove any removed values
            foreach (var removedItem in e.RemovedItems.Cast<string>().Where(removedItem => _selectedMissingValues.Contains(removedItem)))
            {
                _selectedMissingValues.Remove(removedItem);
            }
        }

        #endregion

        private void FindErroneousValues()
        {
            if (SelectedSensor == null)
                return;

            MissingValues.Clear();

            foreach (var detectionMethod in _selectedMethods)
            {
                MissingValues.AddRange(detectionMethod.GetDetectedValues());
            }

            MissingValues = new List<string>(MissingValues);
        }

        private void UpdateDetectionMethodsSettings()
        {
            DetectionSettingsGrid = (SelectedDetectionMethod == null || !SelectedDetectionMethod.HasSettings)
                                        ? null
                                        : SelectedDetectionMethod.SettingsGrid;

            DetectionSettingsVisibility = (SelectedDetectionMethod == null || !SelectedDetectionMethod.HasSettings)
                                              ? Visibility.Collapsed
                                              : Visibility.Visible;

            DetectionSettingsHeight = (SelectedDetectionMethod == null || !SelectedDetectionMethod.HasSettings)
                                          ? new GridLength(0)
                                          : new GridLength(1, GridUnitType.Star);
        }
    }
}
