using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    class ErroneousValuesDetectionViewModel : BaseViewModel
    {
        public ErroneousValuesDetectionViewModel(WindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            DetectionMethods = new List<IDetectionMethod> { new MissingValuesDetector(), new MissingValuesDetector(), new MissingValuesDetector(), new MissingValuesDetector() };

            var behaviours = new BehaviourManager { AllowMultipleEnabled = true };

            var samplingBackground = new GraphBackgroundBehaviour(_samplingBackground) { IsEnabled = true };

            behaviours.Behaviours.Add(samplingBackground);

            var zooming = new CustomZoomBehaviour { IsEnabled = true };

            zooming.ZoomRequested += (o, e) =>
                                         {
                                             var startTime = (DateTime)e.FirstPoint.X;
                                             var endTime = (DateTime)e.SecondPoint.X;
                                             _selectedSensor.SetUpperAndLowerBounds(startTime, endTime);
                                             UpdateGraph();
                                         };
            zooming.ZoomResetRequested += o =>
                                              {
                                                  _selectedSensor.RemoveBounds();
                                                  UpdateGraph();
                                              };

            behaviours.Behaviours.Add(zooming);

            ChartBehaviour = behaviours;

            SamplingCapOptions = new List<string>(Common.GenerateSamplingCaps());
            SelectedSamplingCapIndex = 3;
        }

        #region Private Variables

        private readonly WindowManager _windowManager;
        private readonly SimpleContainer _container;

        private Cursor _cursor = Cursors.Arrow;
        private List<IDetectionMethod> _detectionMethods = new List<IDetectionMethod>();
        private List<GraphableSensor> _sensorList = new List<GraphableSensor>();
        private List<ErroneousValue> _missingValues = new List<ErroneousValue>();
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
        private List<ErroneousValue> _selectedMissingValues = new List<ErroneousValue>();
        private Visibility _detectionSettingsVisibility = Visibility.Collapsed;
        private Grid _detectionSettingsGrid;
        private GridLength _detectionSettingsHeight = new GridLength(0);

        private string _chartTitle = string.Empty;
        private BehaviourManager _chartBehaviour;
        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private string _yAxisTitle = string.Empty;
        private DoubleRange _yAxisRange = new DoubleRange(0, 0);

        private readonly Canvas _samplingBackground = new Canvas { Visibility = Visibility.Collapsed };
        private List<string> _samplingCapOptions = new List<string>();
        private int _selectedSamplingCapIndex;

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

        #region ChartBindings

        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(() => ChartTitle); } }

        public BehaviourManager ChartBehaviour { get { return _chartBehaviour; } set { _chartBehaviour = value; NotifyOfPropertyChange(() => ChartBehaviour); } }

        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }

        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        public DoubleRange YAxisRange { get { return _yAxisRange; } set { _yAxisRange = value; NotifyOfPropertyChange(() => YAxisRange); } }

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

        public List<ErroneousValue> MissingValues { get { return _missingValues; } set { _missingValues = value; NotifyOfPropertyChange(() => MissingValues); } }

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

        #region Sampling Cap

        public List<string> SamplingCapOptions { get { return _samplingCapOptions; } set { _samplingCapOptions = value; NotifyOfPropertyChange(() => SamplingCapOptions); } }

        public int SelectedSamplingCapIndex { get { return _selectedSamplingCapIndex; } set { _selectedSamplingCapIndex = value; NotifyOfPropertyChange(() => SelectedSamplingCapIndex); } }

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

            if (_selectedSensor == null || method == null)
                return;
            foreach (var value in method.GetDetectedValues(_selectedSensor.Sensor))
            {
                var found = false;
                foreach (var erroneousValue in MissingValues.Where(erroneousValue => value.Equals(erroneousValue)))
                {
                    found = true;
                    erroneousValue.Detectors.Add(method);
                    break;
                }
                if(!found)
                    MissingValues.Add(value);
            }

            MissingValues = new List<ErroneousValue>(MissingValues);
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

            if(_selectedMethods.Count == 0)
            {
                MissingValues = new List<ErroneousValue>();
                return;
            }

            for (var i = 0; i < MissingValues.Count; i++)
            {
                var value = MissingValues[i];

                if (!value.Detectors.Contains(method)) continue;

                value.Detectors.Remove(method);

                if (value.Detectors.Count == 0)
                {
                    MissingValues.Remove(value);
                }
            }

            MissingValues = new List<ErroneousValue>(MissingValues);
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
                _selectedMissingValues.Add((ErroneousValue)addedItem);
            }

            //Remove any removed values
            foreach (var removedItem in e.RemovedItems.Cast<ErroneousValue>().Where(removedItem => _selectedMissingValues.Contains(removedItem)))
            {
                _selectedMissingValues.Remove(removedItem);
            }
        }

        public void SamplingCapChanged(SelectionChangedEventArgs e)
        {
            try
            {
                Common.MaximumGraphablePoints = int.Parse((string)e.AddedItems[0]);
            }
            catch (Exception)
            {
                Common.MaximumGraphablePoints = int.MaxValue;
            }

            if (_selectedSensor != null)
                SampleValues(_selectedSensor, Common.MaximumGraphablePoints);
        }

        #endregion

        private void FindErroneousValues()
        {
            if (SelectedSensor == null)
                return;

            MissingValues.Clear();

            foreach (var detectionMethod in _selectedMethods)
            {
                MissingValues.AddRange(detectionMethod.GetDetectedValues(SelectedSensor.Sensor));
            }

            MissingValues = new List<ErroneousValue>(MissingValues);

            UpdateGraph();
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

        #region Graphing Methods

        private void UpdateGraph()
        {
            if (SelectedSensor == null)
                ChartSeries = new List<LineSeries>();
            else
                SampleValues(SelectedSensor, Common.MaximumGraphablePoints);
        }

        private void SampleValues(GraphableSensor sensor, int maxPointCount)
        {
            HideBackground();

            var generatedSeries = new List<LineSeries>();

            var sampleRate = sensor.DataPoints.Count() / maxPointCount;

            Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), maxPointCount, sampleRate);

            var series = (sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);

            generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });

            if (sampleRate > 1)
                ShowBackground();

            ChartSeries = generatedSeries;

            YAxisRange = new DoubleRange(MinimumY() - 10, MaximumY() + 10);
            YAxisTitle = sensor.Sensor.Unit;
        }

        #region GraphBackground Modifiers

        private void HideBackground()
        {
            _samplingBackground.Visibility = Visibility.Collapsed;
        }

        private void ShowBackground()
        {
            _samplingBackground.Visibility = Visibility.Visible;
        }

        #endregion

        #region Range End Point Calculators

        private float MaximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var value in _selectedSensor.DataPoints)
            {
                if (maxY == null)
                    maxY = value;
                else if (value.Y > maxY.Y)
                    maxY = value;
            }

            return maxY == null ? 10 : maxY.Y;
        }

        private float MinimumY()
        {
            DataPoint<DateTime, float> minY = null;

            foreach (var value in _selectedSensor.DataPoints)
            {
                if (minY == null)
                    minY = value;
                else if (value.Y < minY.Y)
                    minY = value;
            }

            return minY == null ? new DataPoint<DateTime, float>(DateTime.Now, 0).Y : minY.Y;
        }

        #endregion

        #endregion
    }
}
