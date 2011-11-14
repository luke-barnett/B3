using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public ErroneousValuesDetectionViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            _minMaxRateofChangeDetector = new MinMaxRateOfChangeDetector();
            _minMaxRateofChangeDetector.GraphUpdateNeeded += UpdateGraph;

            _runningMeanStandardDeviationDetector = new RunningMeanStandardDeviationDetector();
            _runningMeanStandardDeviationDetector.GraphUpdateNeeded += UpdateGraph;

            _runningMeanStandardDeviationDetector.RefreshDetectedValues += delegate
                                                                              {
                                                                                  if (!_selectedMethods.Contains(_runningMeanStandardDeviationDetector))
                                                                                      return;
                                                                                  RemoveDetectionMethod(_runningMeanStandardDeviationDetector);
                                                                                  AddDetectionMethod(_runningMeanStandardDeviationDetector);
                                                                              };

            _missingValuesDetector = new MissingValuesDetector();

            DetectionMethods = new List<IDetectionMethod> { _missingValuesDetector, _minMaxRateofChangeDetector, _runningMeanStandardDeviationDetector };

            var behaviours = new BehaviourManager { AllowMultipleEnabled = true };

            var samplingBackground = new GraphBackgroundBehaviour(_samplingBackground) { IsEnabled = true };

            behaviours.Behaviours.Add(samplingBackground);

            _zooming = new CustomZoomBehaviour { IsEnabled = !_inSelectionMode };

            _zooming.ZoomRequested += (o, e) =>
                                         {
                                             var startTime = (DateTime)e.FirstPoint.X;
                                             var endTime = (DateTime)e.SecondPoint.X;
                                             _selectedSensor.SetUpperAndLowerBounds(startTime, endTime);
                                             CalculateDateTimeEndPoints();
                                         };
            _zooming.ZoomResetRequested += o =>
                                              {
                                                  _selectedSensor.RemoveBounds();
                                                  CalculateDateTimeEndPoints();
                                              };

            behaviours.Behaviours.Add(_zooming);

            _selection = new CustomSelectionBehaviour { IsEnabled = _inSelectionMode };

            _selection.SelectionMade += (o, e) =>
                                           {
                                               _selectionMade = true;
                                               _startSelectionDate = (DateTime)e.FirstPoint.X;
                                               _endSelectionDate = (DateTime)e.SecondPoint.X;
                                               ActionButtonsEnabled = true;
                                           };

            _selection.SelectionReset += o =>
                                            {
                                                _selectionMade = false;
                                                ActionButtonsEnabled = _selectedMissingValues.Count != 0;
                                            };

            behaviours.Behaviours.Add(_selection);

            ChartBehaviour = behaviours;

            SamplingCapOptions = new List<string>(Common.GenerateSamplingCaps());
            SelectedSamplingCapIndex = 3;
        }

        #region Private Variables

        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;

        private Cursor _cursor = Cursors.Arrow;
        private List<IDetectionMethod> _detectionMethods = new List<IDetectionMethod>();
        private List<GraphableSensor> _sensorList = new List<GraphableSensor>();
        private List<ErroneousValue> _foundErroneousValues = new List<ErroneousValue>();
        private bool _showUndoStates;
        private bool _showRedoStates;
        private bool _actionButtonsEnabled;

        private Dataset _dataset;
        private GraphableSensor _selectedSensor;
        private IDetectionMethod _selectedDetectionMethod;
        private Sensor Sensor { get { return (_selectedSensor == null) ? null : _selectedSensor.Sensor; } }
        private readonly List<IDetectionMethod> _selectedMethods = new List<IDetectionMethod>();
        private readonly List<ErroneousValue> _selectedMissingValues = new List<ErroneousValue>();
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

        private double _minimum;
        private double _minimumMinimum;
        private double _maximumMinimum;
        private double _maximum;
        private double _minimumMaximum;
        private double _maximumMaximum;

        private string _waitReason = string.Empty;
        private bool _showRawDataOnGraph;

        private readonly CustomZoomBehaviour _zooming;
        private readonly CustomSelectionBehaviour _selection;

        private bool _inSelectionMode;
        private bool _selectionMade;
        private DateTime _startSelectionDate;
        private DateTime _endSelectionDate;

        private bool _buttonsEnabled = true;
        private bool _canEditDates;
        private DateTime _startTime = DateTime.MinValue;
        private DateTime _endTime = DateTime.MaxValue;

        private readonly MinMaxRateOfChangeDetector _minMaxRateofChangeDetector;
        private readonly RunningMeanStandardDeviationDetector _runningMeanStandardDeviationDetector;
        private readonly MissingValuesDetector _missingValuesDetector;

        #endregion

        #region Visual Items

        public string Title
        {
            get
            {
                if (_selectedSensor == null)
                    return string.Format("[{0}] Erroneous Values Detection", (_dataset != null) ? _sensorList[0].Sensor.Owner.IdentifiableName : Common.UnknownSite);
                else
                    return string.Format("[{0}] Erroneous Values Detection - {1}",
                                            (_dataset != null) ? _sensorList[0].Sensor.Owner.IdentifiableName : Common.UnknownSite, _selectedSensor.Sensor.Name);
            }
        }

        public Cursor ViewCursor { get { return _cursor; } set { _cursor = value; NotifyOfPropertyChange(() => ViewCursor); } }

        public bool ActionButtonsEnabled { get { return (ButtonsEnabled && _actionButtonsEnabled); } set { _actionButtonsEnabled = value; NotifyOfPropertyChange(() => ActionButtonsEnabled); } }

        public Visibility DetectionSettingsVisibility { get { return _detectionSettingsVisibility; } set { _detectionSettingsVisibility = value; NotifyOfPropertyChange(() => DetectionSettingsVisibility); } }

        public Grid DetectionSettingsGrid { get { return _detectionSettingsGrid; } set { _detectionSettingsGrid = value; NotifyOfPropertyChange(() => DetectionSettingsGrid); } }

        public GridLength DetectionSettingsHeight { get { return _detectionSettingsHeight; } set { _detectionSettingsHeight = value; NotifyOfPropertyChange(() => DetectionSettingsHeight); } }

        public string WaitReason { get { return _waitReason; } set { _waitReason = value; NotifyOfPropertyChange(() => WaitReason); NotifyOfPropertyChange(() => WaitReasonVisibility); } }

        public Visibility WaitReasonVisibility { get { return (string.IsNullOrWhiteSpace(WaitReason)) ? Visibility.Collapsed : Visibility.Visible; } }

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
                if (SensorList.Count == 1)
                {
                    SelectedSensor = SensorList[0];
                }
            }
        }

        /// <summary>
        /// Set this if you only want to use a single sensor, which will be automatically selected
        /// </summary>
        public Sensor SingleSensorToUse
        {
            set
            {
                _dataset = value.Owner;
                var sensor = new GraphableSensor(value);
                SensorList = new List<GraphableSensor> { sensor };
                SelectedSensor = sensor;
            }
        }

        public GraphableSensor SelectedSensor
        {
            get { return _selectedSensor; }
            set
            {
                if (ViewCursor != Cursors.Wait)
                {
                    _selectedSensor = value;
                    CalculateDateTimeEndPoints();
                    FindErroneousValues();
                }
                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => Title);
            }
        }

        public IDetectionMethod SelectedDetectionMethod { get { return _selectedDetectionMethod; } set { _selectedDetectionMethod = value; NotifyOfPropertyChange(() => SelectedDetectionMethod); UpdateDetectionMethodsSettings(); } }

        public bool ButtonsEnabled
        {
            get { return _buttonsEnabled; }
            private set
            {
                _buttonsEnabled = value;
                NotifyOfPropertyChange(() => ButtonsEnabled);
                NotifyOfPropertyChange(() => ActionButtonsEnabled);
                NotifyOfPropertyChange(() => UndoButtonEnabled);
                NotifyOfPropertyChange(() => RedoButtonEnabled);
                NotifyOfPropertyChange(() => CanEditDates);
            }
        }

        #region DetectionMethodLimiters

        public void OnlyUseMissingValues()
        {
            _selectedMethods.Clear();
            AddDetectionMethod(_missingValuesDetector);
            _missingValuesDetector.IsEnabled = true;
            DetectionMethods = new List<IDetectionMethod> { _missingValuesDetector };
        }

        public void OnlyUseStandarDeviation()
        {
            _selectedMethods.Clear();
            AddDetectionMethod(_runningMeanStandardDeviationDetector);
            _runningMeanStandardDeviationDetector.IsEnabled = true;
            DetectionMethods = new List<IDetectionMethod> { _runningMeanStandardDeviationDetector };
        }

        public void OnlyUseMinMaxRateOfChange()
        {
            _selectedMethods.Clear();
            AddDetectionMethod(_minMaxRateofChangeDetector);
            _minMaxRateofChangeDetector.IsEnabled = true;
            DetectionMethods = new List<IDetectionMethod> { _minMaxRateofChangeDetector };
        }

        #endregion

        #region Public Lists

        public List<IDetectionMethod> DetectionMethods { get { return _detectionMethods; } set { _detectionMethods = value; NotifyOfPropertyChange(() => DetectionMethods); } }

        public List<GraphableSensor> SensorList { get { return _sensorList; } set { _sensorList = value; NotifyOfPropertyChange(() => SensorList); } }

        public List<ErroneousValue> FoundErroneousValues { get { return _foundErroneousValues; } set { _foundErroneousValues = value; NotifyOfPropertyChange(() => FoundErroneousValues); } }

        #endregion

        #region Undo/Redo

        #region Undo

        public bool UndoButtonEnabled { get { return ButtonsEnabled && SelectedSensor != null && !SelectedSensor.Sensor.CurrentState.IsRaw; } }

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

        public bool RedoButtonEnabled { get { return ButtonsEnabled && SelectedSensor != null && SelectedSensor.Sensor.RedoStates.Count > 0; } }

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

        #region YAxisControls

        /// <summary>
        /// The value of the lower Y Axis range
        /// </summary>
        public double Minimum { get { return _minimum; } set { _minimum = value; NotifyOfPropertyChange(() => Minimum); NotifyOfPropertyChange(() => MinimumValue); MinimumMaximum = Minimum; if (Minimum < Maximum) YAxisRange = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1; } }

        /// <summary>
        /// The minimum value as a readable string
        /// </summary>
        public string MinimumValue
        {
            get { return string.Format("{0:N2}", Minimum); }
            set
            {
                var old = Minimum;
                try
                {
                    Minimum = double.Parse(value);
                }
                catch (Exception)
                {
                    Minimum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the bottom range can reach
        /// </summary>
        public double MaximumMinimum { get { return _maximumMinimum; } set { _maximumMinimum = value; NotifyOfPropertyChange(() => MaximumMinimum); } }

        /// <summary>
        /// The lowest value the bottom range can reach
        /// </summary>
        public double MinimumMinimum { get { return _minimumMinimum; } set { _minimumMinimum = value; NotifyOfPropertyChange(() => MinimumMinimum); } }

        /// <summary>
        /// The value of the high Y Axis range
        /// </summary>
        public double Maximum { get { return _maximum; } set { _maximum = value; NotifyOfPropertyChange(() => Maximum); NotifyOfPropertyChange(() => MaximumValue); MaximumMinimum = Maximum; if (Minimum < Maximum) YAxisRange = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1; } }

        /// <summary>
        /// The maximum value as a readable string
        /// </summary>
        public string MaximumValue
        {
            get { return string.Format("{0:N2}", Maximum); }
            set
            {
                var old = Maximum;
                try
                {
                    Maximum = double.Parse(value);
                }
                catch (Exception)
                {
                    Maximum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the top range can reach
        /// </summary>
        public double MaximumMaximum { get { return _maximumMaximum; } set { _maximumMaximum = value; NotifyOfPropertyChange(() => MaximumMaximum); } }

        /// <summary>
        /// The lowest value the top range can reach
        /// </summary>
        public double MinimumMaximum { get { return _minimumMaximum; } set { _minimumMaximum = value; NotifyOfPropertyChange(() => MinimumMaximum); } }

        #endregion

        #region Sampling Cap

        public List<string> SamplingCapOptions { get { return _samplingCapOptions; } set { _samplingCapOptions = value; NotifyOfPropertyChange(() => SamplingCapOptions); } }

        public int SelectedSamplingCapIndex { get { return _selectedSamplingCapIndex; } set { _selectedSamplingCapIndex = value; NotifyOfPropertyChange(() => SelectedSamplingCapIndex); } }

        #endregion

        #region Date Pickers

        public bool CanEditDates { get { return ButtonsEnabled && _canEditDates; } private set { _canEditDates = value; NotifyOfPropertyChange(() => CanEditDates); } }

        public DateTime StartTime { get { return _startTime; } set { _startTime = value; NotifyOfPropertyChange(() => StartTime); } }

        public DateTime EndTime { get { return _endTime; } set { _endTime = value; NotifyOfPropertyChange(() => EndTime); } }

        #endregion

        #region Event Handlers

        #region Detection Methods

        public void DetectionMethodChecked(RoutedEventArgs eventArgs)
        {
            var checkBox = eventArgs.Source as CheckBox;
            if (checkBox == null) return;

            var method = (checkBox.Content as IDetectionMethod);
            AddDetectionMethod(method);
        }

        public void DetectionMethodUnChecked(RoutedEventArgs eventArgs)
        {
            var checkBox = eventArgs.Source as CheckBox;
            if (checkBox == null) return;

            var method = (checkBox.Content as IDetectionMethod);
            RemoveDetectionMethod(method);
        }

        #endregion

        #region Undo

        public void BtnUndo()
        {
            if (_selectedSensor == null)
                return;

            _selectedSensor.Sensor.Undo();

            UpdateUndoRedo();
        }

        public void UndoPathSelected(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || e.AddedItems[0] == null) return;

            var item = (SensorStateListObject)e.AddedItems[0];

            if (SelectedSensor != null && item != null)
            {
                if (item.IsRaw)
                    SelectedSensor.Sensor.RevertToRaw();
                else
                    SelectedSensor.Sensor.Undo(item.State.EditTimestamp);
            }

            ShowUndoStates = false;
            UpdateUndoRedo();
        }

        #endregion

        #region Redo

        public void BtnRedo()
        {
            if (_selectedSensor == null)
                return;

            _selectedSensor.Sensor.Redo();

            UpdateUndoRedo();
        }

        public void RedoPathSelected(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || e.AddedItems[0] == null) return;

            var item = (SensorStateListObject)e.AddedItems[0];

            if (SelectedSensor != null && item != null)
                SelectedSensor.Sensor.Redo(item.State.EditTimestamp);

            ShowRedoStates = false;
            UpdateUndoRedo();
        }

        #endregion

        public void BtnDone()
        {
            TryClose();
        }

        #region Value Changers

        public void BtnExtrapolate()
        {
            var dates = GetDates();

            if (dates.Count == 0)
                return;

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) => _selectedSensor.Sensor.AddState(_selectedSensor.Sensor.CurrentState.Extrapolate(dates, _selectedSensor.Sensor.Owner));

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             StopWaiting();

                                             Finalise("Extrapolated Values");

                                             Common.ShowMessageBox("Values Updated", "The selected values were extrapolated", false, false);

                                             UpdateUndoRedo();
                                             ButtonsEnabled = true;
                                         };
            ButtonsEnabled = false;
            StartWaiting("Extrapolating Values");
            bw.RunWorkerAsync();
        }

        public void BtnMakeZero()
        {
            var dates = GetDates();

            if (dates.Count == 0)
                return;

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) => _selectedSensor.Sensor.AddState(_selectedSensor.Sensor.CurrentState.MakeZero(dates));

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             StopWaiting();

                                             Finalise("Selected values set to 0.");

                                             Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);

                                             UpdateUndoRedo();
                                             ButtonsEnabled = true;
                                         };
            ButtonsEnabled = false;
            StartWaiting("Setting values to zero");
            EventLogger.LogInfo(_dataset, GetType().ToString(), "Value updation started.");
            bw.RunWorkerAsync();

        }

        public void BtnSpecify()
        {
            var dates = GetDates();

            if (dates.Count == 0)
                return;

            var specifyVal = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
            _windowManager.ShowDialog(specifyVal);

            if (specifyVal == null || specifyVal.Text == null)
                return;
            try
            {
                var value = float.Parse(specifyVal.Text);

                var bw = new BackgroundWorker();

                bw.DoWork += (o, e) => _selectedSensor.Sensor.AddState(_selectedSensor.Sensor.CurrentState.MakeValue(dates, value));

                bw.RunWorkerCompleted += (o, e) =>
                                             {
                                                 StopWaiting();
                                                 Finalise("Selected values has been set to " + value + ".");

                                                 Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);

                                                 UpdateUndoRedo();
                                                 ButtonsEnabled = true;
                                             };
                ButtonsEnabled = false;
                StartWaiting("Setting values to " + value);
                EventLogger.LogInfo(_dataset, GetType().ToString(), "Value updation started.");
                bw.RunWorkerAsync();

            }
            catch (Exception)
            {
                var exit = Common.ShowMessageBox("An Error Occured", "Please enter a valid number.", true, true);
                if (exit) return;
            }


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

            ActionButtonsEnabled = _selectionMade || _selectedMissingValues.Count != 0;
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

        public void GraphRawData()
        {
            _showRawDataOnGraph = true;
            UpdateGraph();
        }

        public void DontGraphRawData()
        {
            _showRawDataOnGraph = false;
            UpdateGraph();
        }

        public void StartSelectionMode()
        {
            _inSelectionMode = true;

            _zooming.IsEnabled = !_inSelectionMode;
            _selection.IsEnabled = _inSelectionMode;
        }

        public void EndSelectionMode()
        {
            _inSelectionMode = false;

            _zooming.IsEnabled = !_inSelectionMode;
            _selection.IsEnabled = _inSelectionMode;
        }

        public void ClosingWindow(CancelEventArgs eventArgs)
        {
            if (!ButtonsEnabled)
                eventArgs.Cancel = true;
        }

        #region Date Pickers

        public void StartTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null)
                return;

            if (e.NewValue == e.OldValue)
                return;

            DateTime newStartTime;

            if (e.OldValue == null || (DateTime)e.OldValue == new DateTime() || (DateTime)e.NewValue < EndTime)
                newStartTime = (DateTime)e.NewValue;
            else
                newStartTime = (DateTime)e.OldValue;

            _selectedSensor.SetUpperAndLowerBounds(newStartTime, EndTime);

            if (SelectedSensor == null)
                ChartSeries = new List<LineSeries>();
            else
                SampleValues(SelectedSensor, Common.MaximumGraphablePoints);
        }

        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null)
                return;

            if (e.NewValue == e.OldValue)
                return;

            DateTime newEndTime;

            if (e.OldValue == null || (DateTime)e.OldValue == new DateTime() || (DateTime)e.NewValue > StartTime)
                newEndTime = (DateTime)e.NewValue;
            else
                newEndTime = (DateTime)e.OldValue;

            _selectedSensor.SetUpperAndLowerBounds(StartTime, newEndTime);

            if (SelectedSensor == null)
                ChartSeries = new List<LineSeries>();
            else
                SampleValues(SelectedSensor, Common.MaximumGraphablePoints);
        }

        #endregion

        public void GraphCenteringRequested(ErroneousValue value)
        {
            Debug.WriteLine("Graph Centering Requested " + value);
            var give = new TimeSpan(0, 1, 0, 0);
            var closestLower = _selectedSensor.Sensor.CurrentState.Values.Select(x => x.Key).Where(x => x < (value.TimeStamp - give)).DefaultIfEmpty(
                _selectedSensor.Sensor.CurrentState.Values.Select(x => x.Key).Min()).Max();
            var closestUpper = _selectedSensor.Sensor.CurrentState.Values.Select(x => x.Key).Where(x => x > (value.TimeStamp + give)).DefaultIfEmpty(
                _selectedSensor.Sensor.CurrentState.Values.Select(x => x.Key).Max()).Min();

            _selectedSensor.SetUpperAndLowerBounds(closestLower, closestUpper);
            CalculateDateTimeEndPoints();

        }

        #endregion

        private void FindErroneousValues()
        {
            if (SelectedSensor == null)
                return;

            FoundErroneousValues.Clear();

            CheckTheseMethods(_selectedMethods);

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
                                          : GridLength.Auto;
        }

        private void Finalise(string taskPerformed)
        {
            foreach (var value in _selectedMissingValues)
            {
                var notErroneous = true;
                foreach (var method in _selectedMethods)
                {
                    notErroneous = !method.CheckIndividualValue(_selectedSensor.Sensor, value.TimeStamp);
                    if (notErroneous)
                    {
                        value.Detectors.RemoveAll(x => x.Equals(method) || x.Children.Contains(method));
                    }
                    else
                    {
                        if (!value.Detectors.Contains(method))
                            value.Detectors.Add(method);
                    }
                }
                if (notErroneous)
                    FoundErroneousValues.Remove(value);
            }
            FoundErroneousValues = new List<ErroneousValue>(FoundErroneousValues);

            SelectedSensor.RefreshDataPoints();

            UpdateGraph();

            Common.RequestReason(_selectedSensor.Sensor, _container, _windowManager, taskPerformed);
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

            var numberOfSeries = 1;

            if (_showRawDataOnGraph)
                numberOfSeries += 1;

            numberOfSeries += _selectedMethods.Where(method => method.HasGraphableSeries).Count();

            var sampleRate = sensor.DataPoints.Count() / (maxPointCount / numberOfSeries);

            Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), maxPointCount, sampleRate);

            //Put Raw Data on bottom
            if (_showRawDataOnGraph)
            {
                generatedSeries.Add(new LineSeries { LineStroke = new SolidColorBrush(sensor.RawDataColour), DataSeries = (sampleRate > 1) ? new DataSeries<DateTime, float>("Raw Data", sensor.RawDataPoints.Where((x, index) => index % sampleRate == 0)) : new DataSeries<DateTime, float>("Raw Data", sensor.RawDataPoints), LineStrokeThickness = 1.5 });
            }

            //Draw the series
            generatedSeries.Add(new LineSeries { DataSeries = (sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints), LineStroke = new SolidColorBrush(sensor.Colour) });

            foreach (var method in _selectedMethods.Where(method => method.HasGraphableSeries))
            {
                Debug.Print("Adding series from {0}", method.Name);

                //Get them
                var methodsSeries = sensor.BoundsSet
                                        ? method.GraphableSeries(sensor.Sensor, sensor.LowerBound,
                                                                 sensor.UpperBound)
                                        : method.GraphableSeries(sensor.Sensor, sensor.Sensor.Owner.StartTimeStamp,
                                                                 sensor.Sensor.Owner.EndTimeStamp);
                //If we need to apply sampling do so
                if (sampleRate > 1)
                {
                    foreach (var x in methodsSeries.Where(i => i.DataSeries.Cast<object>().Count() > (maxPointCount / numberOfSeries)))
                    {
                        x.DataSeries = new DataSeries<DateTime, float>(x.DataSeries.Title,
                                                                       x.DataSeries.Cast<DataPoint<DateTime, float>>().
                                                                           Where
                                                                           ((value, index) => index % sampleRate == 0));
                    }
                }

                //Add them
                generatedSeries.AddRange(methodsSeries);
            }

            if (sampleRate > 1)
                ShowBackground();

            ChartSeries = generatedSeries;

            YAxisTitle = sensor.Sensor.Unit;

            var maxY = MaximumY();
            var minY = MinimumY();

            MaximumMaximum = maxY + (maxY * 0.1);
            MinimumMinimum = minY - Math.Abs(minY * 0.1);

            Maximum = MaximumMaximum;
            Minimum = MinimumMinimum;


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

            foreach (var value in ChartSeries.SelectMany(series => series.DataSeries.Cast<DataPoint<DateTime, float>>()))
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

            foreach (var value in ChartSeries.SelectMany(series => series.DataSeries.Cast<DataPoint<DateTime, float>>()))
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

        private void AddToMissingValues(IEnumerable<ErroneousValue> values)
        {
            var existingValuesDict = FoundErroneousValues.ToDictionary(x => x.TimeStamp);

            foreach (var erroneousValue in values)
            {
                if (existingValuesDict.ContainsKey(erroneousValue.TimeStamp))
                    existingValuesDict[erroneousValue.TimeStamp].Detectors.AddRange(erroneousValue.Detectors);
                else
                {
                    existingValuesDict[erroneousValue.TimeStamp] = erroneousValue;
                    ErroneousValue value = erroneousValue;

                    //Remove any old events
                    erroneousValue.RemoveAllEvents();

                    erroneousValue.GraphCenteringRequested += () => GraphCenteringRequested(value);
                }
            }

            FoundErroneousValues = (from value in existingValuesDict select value.Value).ToList();
        }

        private void CheckTheseMethods(IEnumerable<IDetectionMethod> methods)
        {
            //Go to list to make sure it's not re-enumerating
            methods = methods.ToList();

            if (methods.Count() == 0 || SelectedSensor == null)
                return;

            Debug.WriteLine("We are checking these methods {0}", methods);
            Debug.WriteLine("For this sensor {0}", SelectedSensor);

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
                             {
                                 foreach (var detectionMethod in methods)
                                 {
                                     StartWaiting(string.Format("Checking {0}", detectionMethod.Name));
                                     AddToMissingValues(detectionMethod.GetDetectedValues(SelectedSensor.Sensor));
                                 }
                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             Debug.WriteLine("There are {0} items in Missing Values", FoundErroneousValues.Count);
                                             FoundErroneousValues = new List<ErroneousValue>(FoundErroneousValues);

                                             StopWaiting();
                                         };

            StartWaiting("Looking for values...");
            Debug.WriteLine("Starting check");
            bw.RunWorkerAsync();
        }

        private void UpdateUndoRedo()
        {
            SelectedSensor.RefreshDataPoints();

            UpdateGraph();

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
            NotifyOfPropertyChange(() => UndoStates);
            NotifyOfPropertyChange(() => RedoStates);
            NotifyOfPropertyChange(() => ShowUndoStates);
            NotifyOfPropertyChange(() => ShowRedoStates);
        }

        private void StartWaiting(string waitReason)
        {
            ViewCursor = Cursors.Wait;
            WaitReason = waitReason;
        }

        private void StopWaiting()
        {
            ViewCursor = Cursors.Arrow;
            WaitReason = string.Empty;
        }

        private List<DateTime> GetDates()
        {
            var dates = new List<DateTime>();

            var useRange = false;

            if (_selectionMade)
            {
                var view = _container.GetInstance(typeof(UseSelectedRangeViewModel), "UseSelectedRangeViewModel") as UseSelectedRangeViewModel;

                if (view != null)
                {
                    if (_selectedMissingValues.Count != 0)
                        _windowManager.ShowDialog(view);

                    useRange = view.UseSelectedRange || _selectedMissingValues.Count == 0;
                }
            }

            if (useRange)
                dates.AddRange(from values in _selectedSensor.DataPoints where values.X > _startSelectionDate && values.X < _endSelectionDate select values.X);
            else if (_selectedMissingValues.Count != 0)
                dates.AddRange(from values in _selectedMissingValues select values.TimeStamp);

            return dates;
        }

        private void AddDetectionMethod(IDetectionMethod methodToAdd)
        {
            if (methodToAdd == null)
                return;

            Debug.WriteLine("Adding {0} to selected detection methods", methodToAdd);
            _selectedMethods.Add(methodToAdd);

            SelectedDetectionMethod = methodToAdd;

            if (_selectedSensor == null || methodToAdd == null)
                return;

            CheckTheseMethods(new Collection<IDetectionMethod> { methodToAdd });

            UpdateGraph();
        }

        private void RemoveDetectionMethod(IDetectionMethod methodToRemove)
        {
            if (methodToRemove == null)
                return;
            Debug.WriteLine("Removing {0} from selected detection methods", methodToRemove);
            if (_selectedMethods.Contains(methodToRemove))
                _selectedMethods.Remove(methodToRemove);

            SelectedDetectionMethod = methodToRemove;

            UpdateGraph();

            if (_selectedMethods.Count == 0)
            {
                FoundErroneousValues = new List<ErroneousValue>();
                return;
            }

            FoundErroneousValues.ForEach(
                 x => x.Detectors.RemoveAll(m => m.Equals(methodToRemove) || methodToRemove.Children.Contains(m)));

            FoundErroneousValues = new List<ErroneousValue>(FoundErroneousValues.Where(x => x.Detectors.Count != 0));
        }

        private void CalculateDateTimeEndPoints()
        {
            Debug.Print("Old Start Time {0} Old End Time {1}", StartTime, EndTime);
            var firstOrDefault = _selectedSensor.DataPoints.DefaultIfEmpty(new DataPoint<DateTime, float>(DateTime.MinValue, 0)).FirstOrDefault();
            var lastOrDefault = _selectedSensor.DataPoints.DefaultIfEmpty(new DataPoint<DateTime, float>(DateTime.MaxValue, 0)).LastOrDefault();

            if (firstOrDefault != null && firstOrDefault.X != DateTime.MinValue)
                StartTime = firstOrDefault.X;


            if (lastOrDefault != null && lastOrDefault.X != DateTime.MaxValue)
                EndTime = lastOrDefault.X;
            Debug.Print("New Start Time {0} New End Time {1}", StartTime, EndTime);

            CanEditDates = true;
        }
    }
}
