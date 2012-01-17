using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using GroupBox = System.Windows.Controls.GroupBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;
using SelectionMode = System.Windows.Controls.SelectionMode;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using RadioButton = System.Windows.Controls.RadioButton;
using TabControl = System.Windows.Controls.TabControl;
using TextBox = System.Windows.Controls.TextBox;

namespace IndiaTango.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public MainWindowViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            _sensorsToGraph = new ObservableCollection<GraphableSensor>();
            SensorsToCheckMethodsAgainst = new ObservableCollection<Sensor>();

            _erroneousValuesFromDataTable = new List<ErroneousValue>();

            #region Set Up Detection Methods

            _minMaxDetector = new MinMaxDetector();
            _minMaxDetector.GraphUpdateNeeded += () =>
                                                     {
                                                         SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph,
                                                                      "MinMaxDetectorGraphUpdate");
                                                         CalculateYAxis(false);
                                                     };

            _runningMeanStandardDeviationDetector = new RunningMeanStandardDeviationDetector();
            _runningMeanStandardDeviationDetector.GraphUpdateNeeded += () => SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "RunningMeanGraphUpdate");

            _runningMeanStandardDeviationDetector.RefreshDetectedValues +=
                () => CheckTheseMethods(new Collection<IDetectionMethod> { _runningMeanStandardDeviationDetector });

            _missingValuesDetector = new MissingValuesDetector { IsEnabled = true };
            _selectedMethod = _missingValuesDetector;

            var repeatedValuesDetector = new RepeatedValuesDetector();

            repeatedValuesDetector.RefreshDetectedValues +=
                () => CheckTheseMethods(new Collection<IDetectionMethod> { repeatedValuesDetector });

            _detectionMethods = new List<IDetectionMethod> { _missingValuesDetector, repeatedValuesDetector, _minMaxDetector, new ToHighRateOfChangeDetector(), _runningMeanStandardDeviationDetector };

            #endregion

            #region Set Up Behaviours

            var behaviourManager = new BehaviourManager { AllowMultipleEnabled = true };

            #region Zoom Behaviour
            _zoomBehaviour = new CustomZoomBehaviour { IsEnabled = true };
            _zoomBehaviour.ZoomRequested += (o, e) =>
                                                {
                                                    StartTime = e.LowerX;
                                                    EndTime = e.UpperX;
                                                    Range = new DoubleRange(e.LowerY, e.UpperY);
                                                    foreach (var detectionMethod in _detectionMethods.Where(x => x.IsEnabled))
                                                    {
                                                        var itemsToKeep =
                                                            detectionMethod.ListBox.Items.Cast<ErroneousValue>().Where(
                                                                x => x.TimeStamp >= StartTime && x.TimeStamp <= EndTime)
                                                                .ToList();
                                                        detectionMethod.ListBox.Items.Clear();
                                                        itemsToKeep.ForEach(x => detectionMethod.ListBox.Items.Add(x));
                                                    }
                                                    foreach (var sensor in _sensorsToGraph)
                                                    {
                                                        sensor.SetUpperAndLowerBounds(StartTime, EndTime);
                                                    }
                                                    SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "Zoom");
                                                };
            _zoomBehaviour.ZoomResetRequested += o =>
                                                     {
                                                         foreach (var sensor in _sensorsToGraph)
                                                         {
                                                             sensor.RemoveBounds();
                                                         }
                                                         CalculateGraphedEndPoints();
                                                         SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "ZoomReset");
                                                         CalculateYAxis();
                                                         CheckTheseMethods(_detectionMethods.Where(x => x.IsEnabled));
                                                     };

            behaviourManager.Behaviours.Add(_zoomBehaviour);
            #endregion

            #region Background Behaviour
            _background = new Canvas { Visibility = Visibility.Collapsed };
            var backgroundBehaviour = new GraphBackgroundBehaviour(_background);
            behaviourManager.Behaviours.Add(backgroundBehaviour);
            #endregion

            #region Selection Behaviour

            _selectionBehaviour = new CustomSelectionBehaviour { IsEnabled = true };
            _selectionBehaviour.SelectionMade += (sender, args) =>
                                                     {
                                                         Selection = args;
                                                     };

            _selectionBehaviour.SelectionReset += sender =>
                                                      {
                                                          Selection = null;
                                                      };
            behaviourManager.Behaviours.Add(_selectionBehaviour);
            #endregion

            _dateAnnotator = new DateAnnotationBehaviour { IsEnabled = true };
            behaviourManager.Behaviours.Add(_dateAnnotator);

            _calibrationAnnotator = new CalibrationAnnotatorBehaviour(this) { IsEnabled = true };
            behaviourManager.Behaviours.Add(_calibrationAnnotator);

            behaviourManager.Behaviours.Add(new ChangesAnnotatorBehaviour(this) { IsEnabled = true });

            Behaviour = behaviourManager;

            #endregion

            PropertyChanged += (o, e) =>
                                   {
                                       if (e.PropertyName == "Selection")
                                           ActionsEnabled = Selection != null;
                                   };

            BuildDetectionMethodTabItems();
        }

        #region Private Parameters

        /// <summary>
        /// The Window Manger from Caliburn Micro
        /// </summary>
        private readonly IWindowManager _windowManager;
        /// <summary>
        /// The container holding all the views
        /// </summary>
        private readonly SimpleContainer _container;
        /// <summary>
        /// The current data set being used
        /// </summary>
        private Dataset _currentDataset;
        private string[] _dataSetFiles;
        private int _chosenSelectedIndex;
        #region Progress Values

        private int _progressValue;
        private bool _progressIndeterminate;
        private bool _showProgressArea;
        private string _waitText;

        #endregion
        private string _title = "B3";
        #region Chart
        private List<LineSeries> _chartSeries;
        private BehaviourManager _behaviour;
        private string _chartTitle;
        private string _yAxisTitle;
        private DoubleRange _range;
        private readonly ObservableCollection<GraphableSensor> _sensorsToGraph;
        public readonly ObservableCollection<Sensor> SensorsToCheckMethodsAgainst;
        private int _sampleRate;
        private DateTime _startTime = DateTime.MinValue;
        private DateTime _endTime = DateTime.MaxValue;
        private int _samplingOptionIndex = 3;
        private readonly Canvas _background;
        #region YAxisControls
        private int _minMinimum;
        private int _maxMinimum;
        private int _minMaximum;
        private int _maxMaximum;
        #endregion
        #endregion
        private bool _featuresEnabled = true;
        private List<TabItem> _detectionTabItems;
        private readonly List<IDetectionMethod> _detectionMethods;
        private readonly MinMaxDetector _minMaxDetector;
        private readonly RunningMeanStandardDeviationDetector _runningMeanStandardDeviationDetector;
        private readonly MissingValuesDetector _missingValuesDetector;
        private List<GraphableSensor> _graphableSensors;
        private FormulaEvaluator _evaluator;
        private bool _canUndo;
        private bool _canRedo;
        private bool _showRaw;
        private readonly CustomZoomBehaviour _zoomBehaviour;
        private readonly CustomSelectionBehaviour _selectionBehaviour;
        private readonly DateAnnotationBehaviour _dateAnnotator;
        private readonly CalibrationAnnotatorBehaviour _calibrationAnnotator;
        private SelectionMadeArgs _selection;
        private bool _actionsEnabled;
        private bool _detectionMethodsEnabled;
        private IDetectionMethod _selectedMethod;
        private bool _revertGraphedToRawIsVisible;
        private bool _previousActionsStatus;
        private bool _useFullYAxis;
        private bool _graphEnabled = true;
        private bool _viewAllSensors;
        private DataTable _dataTable;
        private DataTable _summaryStatistics;
        private readonly List<ErroneousValue> _erroneousValuesFromDataTable;
        private bool _notInCalibrationMode = true;

        #endregion

        #region Public Parameters
        #endregion

        #region Private Properties

        private Dataset CurrentDataset
        {
            get { return _currentDataset; }
            set
            {
                _currentDataset = value;
                Debug.WriteLine("Updating for new Dataset");
                if (Sensors == null)
                    CurrentDataset.Sensors = new List<Sensor>();
                if (Sensors == null)
                    return;
                if (Sensors.FirstOrDefault(x => x.Variable == null) != null)
                {
                    var sensorVariables = SensorVariable.CreateSensorVariablesFromSensors(Sensors);
                    foreach (var sensor in Sensors)
                    {
                        sensor.Variable = sensorVariables.FirstOrDefault(x => x.Sensor == sensor);
                    }
                }
                NotifyOfPropertyChange(() => CurrentDataSetNotNull);
                if (CurrentDataSetNotNull)
                {
                    _evaluator = new FormulaEvaluator(Sensors, CurrentDataset.DataInterval);
                    _dateAnnotator.DataInterval = CurrentDataset.DataInterval;
                }
                UpdateGUI();
                UpdateUndoRedo();
                UpdateDataTable();
            }
        }

        private string[] DataSetFiles
        {
            get { return _dataSetFiles ?? (_dataSetFiles = Dataset.GetAllDataSetFileNames()); }
        }

        private List<Sensor> SensorsForEditing
        {
            get { return SensorsToCheckMethodsAgainst.ToList(); }
        }

        private bool CurrentDataSetNotNull
        {
            get { return CurrentDataset != null; }
        }

        private SelectionMadeArgs Selection
        {
            get { return _selection; }
            set
            {
                _selection = value;
                NotifyOfPropertyChange(() => Selection);
            }
        }

        private bool RevertGraphedButtonIsVisible
        {
            set
            {
                _revertGraphedToRawIsVisible = value;
                NotifyOfPropertyChange(() => RevertGraphedToRawVisibility);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The list of site names
        /// </summary>
        public string[] SiteNames
        {
            get
            {
                var siteNamesList = DataSetFiles.Select(x => x.Substring(x.LastIndexOf('\\') + 1, x.Length - x.LastIndexOf('\\') - 4)).ToList();
                siteNamesList.Insert(0, "Create new site...");
                var siteNames = siteNamesList.ToArray();
                if (CurrentDataset != null && DataSetFiles.Contains(CurrentDataset.SaveLocation))
                    ChosenSelectedIndex = Array.IndexOf(DataSetFiles, CurrentDataset.SaveLocation) + 1;
                return siteNames;
            }
        }

        /// <summary>
        /// The currently selected site index
        /// </summary>
        public int ChosenSelectedIndex
        {
            get { return (CurrentDataset != null) ? _chosenSelectedIndex : -1; }
            set
            {
                _chosenSelectedIndex = value;
                NotifyOfPropertyChange(() => ChosenSelectedIndex);
            }
        }

        #region Progress Values

        /// <summary>
        /// The current value of the progress bar
        /// </summary>
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                NotifyOfPropertyChange(() => ProgressValue);
            }
        }

        /// <summary>
        /// Whether or not the progress is indeterminate
        /// </summary>
        public bool ProgressIndeterminate
        {
            get { return _progressIndeterminate; }
            set
            {
                _progressIndeterminate = value;
                NotifyOfPropertyChange(() => ProgressIndeterminate);
            }
        }

        /// <summary>
        /// The string to describe the progress
        /// </summary>
        public string WaitEventString
        {
            get { return _waitText; }
            set
            {
                _waitText = value;
                NotifyOfPropertyChange(() => WaitEventString);
                Debug.Print("Wait Event String set to \"{0}\"", WaitEventString);
            }
        }

        /// <summary>
        /// Whether or not to show the progress area
        /// </summary>
        public bool ShowProgressArea
        {
            get { return _showProgressArea; }
            set
            {
                _showProgressArea = value;
                NotifyOfPropertyChange(() => ProgressAreaVisibility);
            }
        }

        /// <summary>
        /// The visibility of the progress area
        /// </summary>
        public Visibility ProgressAreaVisibility
        {
            get { return ShowProgressArea ? Visibility.Visible : Visibility.Collapsed; }
        }

        #endregion

        /// <summary>
        /// The Title to show for the window
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyOfPropertyChange(() => Title); }
        }

        /// <summary>
        /// The Sensors for the currently selected dataset
        /// </summary>
        public List<Sensor> Sensors
        {
            get { return (CurrentDataset != null) ? CurrentDataset.Sensors : new List<Sensor>(); }
        }

        /// <summary>
        /// The current datasets sensors as Graphable Sensors
        /// </summary>
        public List<GraphableSensor> GraphableSensors
        {
            get { return _graphableSensors ?? (_graphableSensors = (from sensor in Sensors select new GraphableSensor(sensor)).ToList()); }
        }

        #region Charting

        /// <summary>
        /// The list of Line Series that the Chart pulls from
        /// </summary>
        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }
        /// <summary>
        /// The Behaviour Manager for the Chart
        /// </summary>
        public BehaviourManager Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }
        /// <summary>
        /// The Chart Title
        /// </summary>
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(() => ChartTitle); } }
        /// <summary>
        /// The YAxis label for the chart
        /// </summary>
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }
        /// <summary>
        /// The YAxis range on the graph
        /// </summary>
        public DoubleRange Range
        {
            get { return _range; }
            set
            {
                _range = value;
                NotifyOfPropertyChange(() => Range);
                NotifyOfPropertyChange(() => Minimum);
                NotifyOfPropertyChange(() => Maximum);
            }
        }
        /// <summary>
        /// The start of the time period being displayed
        /// </summary>
        public DateTime StartTime { get { return _startTime; } set { _startTime = value; NotifyOfPropertyChange(() => StartTime); } }
        /// <summary>
        /// The end of the time period being displayed
        /// </summary>
        public DateTime EndTime { get { return _endTime; } set { _endTime = value; NotifyOfPropertyChange(() => EndTime); } }
        /// <summary>
        /// Determines if the date range should be shown based on if things are being graphed or not
        /// </summary>
        public bool CanEditDates { get { return (_sensorsToGraph.Count > 0 && FeaturesEnabled); } }

        #region YAxisControls

        public int MaxMaximum { get { return _maxMaximum; } set { _maxMaximum = value; NotifyOfPropertyChange(() => MaxMaximum); } }

        public int MinMaximum { get { return _minMaximum; } set { _minMaximum = value; NotifyOfPropertyChange(() => MinMaximum); } }

        public int MaxMinimum { get { return _maxMinimum; } set { _maxMinimum = value; NotifyOfPropertyChange(() => MaxMinimum); } }

        public int MinMinimum { get { return _minMinimum; } set { _minMinimum = value; NotifyOfPropertyChange(() => MinMinimum); } }

        public float Minimum
        {
            get { return Range != null ? (float)Range.Minimum : 0; }
            set
            {
                if (value < MinMinimum)
                {
                    Range = new DoubleRange(MinMinimum, Range.Maximum);
                }
                else if (value > MaxMinimum)
                {
                    Range = new DoubleRange(MaxMinimum, Range.Maximum);
                }
                else
                {
                    Range = value < Range.Maximum ? new DoubleRange(value, Range.Maximum) : new DoubleRange(Range.Maximum, value);
                }
                MinMaximum = (int)Math.Ceiling(Minimum);
                NotifyOfPropertyChange(() => Minimum);
            }
        }

        public float Maximum
        {
            get { return Range != null ? (float)Range.Maximum : 0; }
            set
            {
                if (value < MinMaximum)
                {
                    Range = new DoubleRange(Range.Minimum, MinMaximum);
                }
                else if (value > MaxMaximum)
                {
                    Range = new DoubleRange(Range.Minimum, MaxMaximum);
                }
                else
                {
                    Range = value > Range.Minimum ? new DoubleRange(Range.Minimum, value) : new DoubleRange(value, Range.Minimum);
                }
                MaxMinimum = (int)Maximum;
                NotifyOfPropertyChange(() => Maximum);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// The available sampling options
        /// </summary>
        public List<string> SamplingOptions
        {
            get { return Common.GenerateSamplingCaps(); }
        }

        /// <summary>
        /// The selected index into the sampling options
        /// </summary>
        public int SamplingOptionIndex
        {
            get { return _samplingOptionIndex; }
            set
            {
                _samplingOptionIndex = value;
                NotifyOfPropertyChange(() => SamplingOptionIndex);
                try
                {
                    Common.MaximumGraphablePoints = int.Parse(SamplingOptions[SamplingOptionIndex]);
                }
                catch
                {
                    Common.MaximumGraphablePoints = int.MaxValue;
                }

                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "SamplingCountChanged");
            }
        }

        /// <summary>
        /// Trigger to disable/enable controls
        /// </summary>
        public bool FeaturesEnabled
        {
            get { return _featuresEnabled; }

            set
            {
                _featuresEnabled = value;
                NotifyOfPropertyChange(() => FeaturesEnabled);
                NotifyOfPropertyChange(() => CanEditDates);
            }
        }

        /// <summary>
        /// The List of Tab Items to show in the Detection Methods
        /// </summary>
        public List<TabItem> DetectionTabItems
        {
            get { return _detectionTabItems ?? new List<TabItem>(); }
            set
            {
                _detectionTabItems = value;
                NotifyOfPropertyChange(() => DetectionTabItems);
            }
        }

        public bool GraphRawData
        {
            get { return _showRaw; }
            set
            {
                _showRaw = value;
                UpdateGraph(false);
                RevertGraphedButtonIsVisible = _showRaw;
            }
        }

        public bool SelectionModeEnabled
        {
            get { return _selectionBehaviour.IsEnabled; }
            set
            {
                _selectionBehaviour.IsEnabled = value;
                _zoomBehaviour.IsEnabled = !value;
                if (!value)
                {
                    _selectionBehaviour.ResetSelection();
                }
            }
        }

        public bool ActionsEnabled
        {
            get { return _actionsEnabled; }
            set
            {
                _actionsEnabled = value;
                NotifyOfPropertyChange(() => ActionsEnabled);
            }
        }

        public bool CanUndo
        {
            get { return _canUndo; }
            set
            {
                _canUndo = value;
                NotifyOfPropertyChange(() => CanUndo);
            }
        }

        public bool CanRedo
        {
            get { return _canRedo; }
            set
            {
                _canRedo = value;
                NotifyOfPropertyChange(() => CanRedo);
            }
        }

        public Visibility RevertGraphedToRawVisibility
        {
            get { return _revertGraphedToRawIsVisible ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool AnnotationsModeEnabled
        {
            get { return _dateAnnotator.IsEnabled; }
            set
            {
                _dateAnnotator.IsEnabled = value;
                _calibrationAnnotator.IsEnabled = value;
            }
        }

        public DataTable DataTable
        {
            get { return _dataTable ?? new DataTable(); }
        }

        public DataTable SummaryStatistics
        {
            get { return _summaryStatistics ?? new DataTable(); }
        }

        public string TotalDataCount
        {
            get { return (ViewAllSensors) ? Sensors.Sum(x => x.CurrentState.Values.Count(y => y.Key >= StartTime && y.Key <= EndTime)).ToString(CultureInfo.InvariantCulture) : SensorsToCheckMethodsAgainst.Sum(x => x.CurrentState.Values.Count(y => y.Key >= StartTime && y.Key <= EndTime)).ToString(CultureInfo.InvariantCulture); }
        }

        public bool ApplyToAllSensors { get; set; }

        public bool FixYAxis
        {
            get { return !NotInCalibrationMode || _selectionBehaviour.UseFullYAxis; }
            set
            {
                if (!NotInCalibrationMode) return;

                _selectionBehaviour.UseFullYAxis = value;
                _useFullYAxis = value;
            }
        }

        public bool ViewAllSensors
        {
            get { return _viewAllSensors; }
            set
            {
                _viewAllSensors = value;
                NotifyOfPropertyChange(() => ViewAllSensors);
                UpdateDataTable();
            }
        }

        public bool NotInCalibrationMode
        {
            get { return _notInCalibrationMode; }
            set
            {
                _notInCalibrationMode = value;
                NotifyOfPropertyChange(() => NotInCalibrationMode);
                NotifyOfPropertyChange(() => FixYAxis);
            }
        }

        public bool UncheckAllSensors
        {
            get { return _sensorsToGraph.Count > 0; }
            set
            {
                if(!value)
                {
                    foreach(var gSensor in GraphableSensors.Where(x => x.IsChecked).ToArray())
                    {
                        gSensor.IsChecked = false;
                    }
                }
                else
                    NotifyOfPropertyChange(() => UncheckAllSensors);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateGUI()
        {
            NotifyOfPropertyChange(() => Sensors);
            var checkedSensors = GraphableSensors.Where(x => x.IsChecked).ToArray();
            _graphableSensors = null;

            foreach (var sensor in SensorsToCheckMethodsAgainst)
            {
                sensor.PropertyChanged -= SensorPropertyChanged;
            }
            SensorsToCheckMethodsAgainst.Clear();
            _sensorsToGraph.Clear();

            foreach (var gSensor in checkedSensors.Where(x => Sensors.Contains(x.Sensor)))
            {
                var sensorToCheck = GraphableSensors.FirstOrDefault(x => gSensor.Sensor == x.Sensor);
                if (sensorToCheck == null) continue;

                sensorToCheck.IsChecked = true;
                AddToGraph(sensorToCheck, false);
            }

            if (!Thread.CurrentThread.IsBackground)
                UpdateGraph(true);

            NotifyOfPropertyChange(() => GraphableSensors);
        }

        private void UpdateDataTable()
        {
            if (_graphEnabled) return;

            var bw = new BackgroundWorker();
            bw.DoWork += (o, e) =>
                             {
                                 _dataTable = (ViewAllSensors) ? DataGridHelper.GenerateDataTable(Sensors, StartTime, EndTime) : DataGridHelper.GenerateDataTable(SensorsToCheckMethodsAgainst, StartTime, EndTime);
                                 _summaryStatistics = (ViewAllSensors) ? new DataTable() : DataGridHelper.GenerateSummaryStatistics(SensorsToCheckMethodsAgainst, StartTime, EndTime);
                             };
            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             NotifyOfPropertyChange(() => DataTable);
                                             NotifyOfPropertyChange(() => TotalDataCount);
                                             NotifyOfPropertyChange(() => SummaryStatistics);
                                             EnableFeatures();
                                             ShowProgressArea = false;
                                         };
            DisableFeatures();
            ShowProgressArea = true;
            ProgressIndeterminate = true;
            WaitEventString = "Generating Table";
            bw.RunWorkerAsync();
        }

        private void SampleValues(int numberOfPoints, ICollection<GraphableSensor> sensors, string sender)
        {
            if (!_graphEnabled)
            {
                Debug.Print("{0} tried to sample graph but graph is disabled instead going to update the grid", sender);
                ChartSeries = new List<LineSeries>();
                UpdateDataTable();
                return;
            }
            Debug.Print("[{0}]Sampling Values", sender);
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            var numberOfExtraLinesToTakeIntoConsiderationWhenSampling = 0;

            if (sensors.Count > 0)
                numberOfExtraLinesToTakeIntoConsiderationWhenSampling += _detectionMethods.Where(x => x.IsEnabled && x.HasGraphableSeries).Sum(detectionMethod => (from lineSeries in detectionMethod.GraphableSeries(sensors.ElementAt(0).Sensor, StartTime, EndTime) select lineSeries.DataSeries.Cast<DataPoint<DateTime, float>>().Count() into numberInLineSeries let numberInSensor = sensors.ElementAt(0).DataPoints.Count() select numberInLineSeries / (double)numberInSensor).Count(percentage => percentage > 0.2d));

            if (_showRaw)
                numberOfExtraLinesToTakeIntoConsiderationWhenSampling += sensors.Count;

            numberOfExtraLinesToTakeIntoConsiderationWhenSampling += sensors.Count(graphableSensor => graphableSensor.PreviewDataPoints != null);

            Debug.Print("There are {0} lines that have been counted as sensors for sampling", numberOfExtraLinesToTakeIntoConsiderationWhenSampling);

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count() / (numberOfPoints / (sensors.Count + numberOfExtraLinesToTakeIntoConsiderationWhenSampling));
                Debug.Print("[{3}] Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate, sensor.Sensor.Name);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                if (_showRaw)
                {
                    var rawSeries = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name + "[RAW]", sensor.RawDataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name + "[RAW]", sensor.RawDataPoints);
                    generatedSeries.Add(new LineSeries { DataSeries = rawSeries, LineStroke = new SolidColorBrush(sensor.RawDataColour) });
                }
                generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
                if (sensor.PreviewDataPoints != null)
                {
                    var previewSeries = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name + "[PREVIEW]", sensor.PreviewDataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name + "[PREVIEW]", sensor.PreviewDataPoints);
                    var colour = sensor.Colour;
                    colour.A = 170;
                    generatedSeries.Add(new LineSeries { DataSeries = previewSeries, LineStroke = new SolidColorBrush(colour) });
                }
                if (_sampleRate > 1) ShowBackground();
            }

            foreach (var detectionMethod in _detectionMethods.Where(x => x.IsEnabled && x.HasGraphableSeries))
            {
                generatedSeries.AddRange(detectionMethod.GraphableSeries(StartTime, EndTime));
            }

            ChartSeries = generatedSeries;
        }

        private void HideBackground()
        {
            _background.Visibility = Visibility.Collapsed;
        }

        private void ShowBackground()
        {
            _background.Visibility = Visibility.Visible;
        }

        private void CalculateGraphedEndPoints()
        {
            var minimum = DateTime.MaxValue;
            var maximum = DateTime.MinValue;

            foreach (var sensor in _sensorsToGraph)
            {
                if (!sensor.DataPoints.Any()) continue;

                var first = sensor.DataPoints.First().X;
                var last = sensor.DataPoints.Last().X;

                if (first < minimum)
                    minimum = first;

                if (last > maximum)
                    maximum = last;
            }

            if (minimum > maximum)
            {
                var temp = minimum;
                minimum = maximum;
                maximum = temp;
            }

            Debug.WriteLine("Calculated the first point {0} and the last point {1}", minimum, maximum);
            StartTime = minimum;
            EndTime = maximum;
            Debug.WriteLine("As a result start {0} and end {1}", StartTime, EndTime);
        }

        private bool ShowSiteInformation(Dataset dataSetToShow)
        {
            if (dataSetToShow == null)
            {
                Common.ShowMessageBox("No Site Selected",
                                      "To view site information you must first select or create a site", false, false);
                return false;
            }

            var view = _container.GetInstance(typeof(EditSiteDataViewModel), "EditSiteDataViewModel") as EditSiteDataViewModel;

            if (view == null)
            {
                EventLogger.LogError(null, "Loading Site Editor", "Critical! Failed to get a View!!");
                return false;
            }

            view.DataSet = dataSetToShow;

            if (dataSetToShow.Site.PrimaryContact == null)
                view.IsNewSite = true;

            view.Deactivated += (o, e) =>
            {
                _dataSetFiles = null;
                NotifyOfPropertyChange(() => SiteNames);
            };

            _windowManager.ShowDialog(view);
            return view.WasCompleted;
        }

        private double MinimumY(IEnumerable<LineSeries> series)
        {
            double[] min = { double.MaxValue };
            foreach (var value in series.SelectMany(line => ((DataSeries<DateTime, float>)line.DataSeries).Where(value => value.Y < min[0])))
            {
                min[0] = value.Y;
            }
            return min[0];
        }

        private double MaximumY(IEnumerable<LineSeries> series)
        {
            double[] max = { double.MinValue };
            foreach (var value in series.SelectMany(line => ((DataSeries<DateTime, float>)line.DataSeries).Where(value => value.Y > max[0])))
            {
                max[0] = value.Y;
            }
            return max[0];
        }

        private void DisableFeatures()
        {
            FeaturesEnabled = false;
            ApplicationCursor = Cursors.Wait;
        }

        private void EnableFeatures()
        {
            FeaturesEnabled = true;
            ApplicationCursor = Cursors.Arrow;
        }

        private void BuildDetectionMethodTabItems()
        {
            var tabItems = _detectionMethods.Select(GenerateTabItemFromDetectionMethod).ToList();
            tabItems.Add(GenerateCalibrationTabItem());
            DetectionTabItems = tabItems;
        }

        private TabItem GenerateTabItemFromDetectionMethod(IDetectionMethod method)
        {
            var tabItem = new TabItem { Header = new TextBlock { Text = method.Abbreviation }, IsEnabled = FeaturesEnabled };

            var tabItemGrid = new Grid();
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock { Text = method.Name, FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(3), TextWrapping = TextWrapping.Wrap };

            Grid.SetRow(title, 0);

            tabItemGrid.Children.Add(title);

            var aboutButton = new Button
                                  {
                                      Content = new Image
                                                    {
                                                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/help_32.png", UriKind.Absolute)),
                                                        Width = 16,
                                                        Height = 16
                                                    },
                                      Margin = new Thickness(3),
                                      HorizontalAlignment = HorizontalAlignment.Right,
                                      HorizontalContentAlignment = HorizontalAlignment.Center
                                  };
            aboutButton.Click +=
                (o, e) => Common.ShowMessageBox(string.Format("  About {0}  ", method.Name), method.About, false, false);

            Grid.SetRow(aboutButton, 0);

            tabItemGrid.Children.Add(aboutButton);

            var detectionMethodOptions = new GroupBox { Header = new TextBlock { Text = "Options" }, BorderBrush = Brushes.OrangeRed };

            var optionsStackPanel = new StackPanel { Orientation = Orientation.Vertical };
            detectionMethodOptions.Content = optionsStackPanel;

            var detectionMethodListBox = new GroupBox { Header = new TextBlock { Text = "Detected Values" }, BorderBrush = Brushes.OrangeRed };
            var settingsGrid = method.SettingsGrid;
            var listBox = new ListBox { SelectionMode = SelectionMode.Extended, IsEnabled = method.IsEnabled };

            settingsGrid.IsEnabled = method.IsEnabled;

            optionsStackPanel.Children.Add(settingsGrid);

            Grid.SetRow(detectionMethodOptions, 1);

            if (method.HasSettings)
                tabItemGrid.Children.Add(detectionMethodOptions);

            method.ListBox = listBox;

            detectionMethodListBox.Content = listBox;

            listBox.SelectionChanged += (o, e) =>
                                            {
                                                if (e.AddedItems.Count > 0)
                                                    _selectionBehaviour.ResetSelection();
                                            };

            _selectionBehaviour.SelectionMade += (o, e) => listBox.UnselectAll();

            Grid.SetRow(detectionMethodListBox, 2);

            tabItemGrid.Children.Add(detectionMethodListBox);

            listBox.SelectionChanged += (o, e) =>
                                            {
                                                var box = o as ListBox;
                                                if (box != null)
                                                    ActionsEnabled = Selection != null || box.SelectedItems.Count > 0;
                                                else
                                                    ActionsEnabled = Selection != null;
                                            };

            tabItem.Content = tabItemGrid;
            return tabItem;
        }

        private TabItem GenerateCalibrationTabItem()
        {
            var tabItem = new TabItem { Header = "Calibration", IsEnabled = FeaturesEnabled };

            //Build the Grid to base it all on and add it
            var tabItemGrid = new Grid();
            tabItem.Content = tabItemGrid;

            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); //Formula area

            var contentGrid = new Grid();
            Grid.SetRow(contentGrid, 0);
            tabItemGrid.Children.Add(contentGrid);

            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var aboutButton = new Button
            {
                Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/help_32.png", UriKind.Absolute)),
                    Width = 16,
                    Height = 16
                },
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Right,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            aboutButton.Click +=
                (o, e) => Common.ShowMessageBox(string.Format("  About Calibration"), "This method is used to post-calibrate a range of data using a mathematical formula. It can also be used to perform unit conversions on sensor data.\r\n\nYou may select a range of data to modify by holding the shift key, and click-dragging the mouse over a range of data on the graph to the right. If no range is selected, then the formula will be applied to the date range that is displayed on the graph.\r\n\nUse ‘Formula’ mode to apply an equation to the data. Each sensor has an identifier (‘Variable’ in the sensor metadata) that can be used in formulas. Formulas can also relate one sensor to another (e.g. to calculate dissolved oxygen concentration from % saturation and water temperature measurements). If you wish to retain the original data, you can create a new sensor and use formula mode to derive values for it.\r\n\nUse ‘Drift adjustment’ mode to adjust for linear drift between sensor calibrations. Calibration logs can be stored in sensor metadata. You can enter a two point calibration (e.g. 0% and 100% following dissolved oxygen sensor calibration), and then enter corresponding values at the end of a period of drift (e.g. 4.3% and 115% after six months of dissolved oxygen sensor deployment). Click ‘Apply’ and the data over the period selected will have a linear back-correction applied in order to compensate for (assumed linear) drift in the electronic response of the instrument.\r\n\nYou can use ‘Preview’ to view the calibration change before applying it.", false, false);
            Grid.SetRow(aboutButton, 0);
            contentGrid.Children.Add(aboutButton);

            var calibrationMethodStackPanel = new StackPanel { Margin = new Thickness(5), Orientation = Orientation.Horizontal };
            Grid.SetRow(calibrationMethodStackPanel, 0);
            contentGrid.Children.Add(calibrationMethodStackPanel);
            calibrationMethodStackPanel.Children.Add(new TextBlock
                                                         {
                                                             Text = "Calibration Method:    "
                                                         });
            var useManualCalibrationRadio = new RadioButton
                                                {
                                                    Content = "Formula    ",
                                                    IsChecked = true
                                                };
            var manualAutoTabControl = new TabControl
                                            {
                                                Padding = new Thickness(0),
                                                Margin = new Thickness(5),
                                                BorderThickness = new Thickness(0),
                                                TabStripPlacement = Dock.Top,
                                                ItemContainerStyle = Application.Current.FindResource("HiddenTabHeaders") as Style
                                            };
            useManualCalibrationRadio.Checked += (o, e) =>
                                                     {
                                                         manualAutoTabControl.SelectedIndex = 0;
                                                     };
            useManualCalibrationRadio.Unchecked += (o, e) =>
                                                       {
                                                           manualAutoTabControl.SelectedIndex = 1;
                                                       };
            calibrationMethodStackPanel.Children.Add(useManualCalibrationRadio);
            calibrationMethodStackPanel.Children.Add(new RadioButton
                                                         {
                                                             Content = "Drift Adjustment"
                                                         });
            Grid.SetRow(manualAutoTabControl, 1);
            contentGrid.Children.Add(manualAutoTabControl);

            #region Manual Tab

            Formula formula = null;

            var manualTabItem = new TabItem
                                    {
                                        Header = "Manual"
                                    };
            manualAutoTabControl.Items.Add(manualTabItem);

            var manualTabGrid = new Grid();
            manualTabItem.Content = manualTabGrid;

            manualTabGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            manualTabGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            manualTabGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var manualTextBlock = new TextBlock
                                      {
                                          Text = "Enter Formula Below:",
                                          Margin = new Thickness(0, 5, 0, 5)
                                      };
            Grid.SetRow(manualTextBlock, 0);
            manualTabGrid.Children.Add(manualTextBlock);

            var manualFormulaTextBox = new TextBox
                                           {
                                               BorderBrush = Brushes.OrangeRed,
                                               Margin = new Thickness(0, 0, 0, 10),
                                               VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                               AcceptsReturn = true,
                                               IsEnabled = CurrentDataSetNotNull
                                           };
            PropertyChanged += (o, e) =>
                                   {
                                       if (e.PropertyName == "CurrentDataSetNotNull")
                                       {
                                           manualFormulaTextBox.IsEnabled = CurrentDataSetNotNull;
                                       }
                                   };
            var applyFormulaButton = new Button
                                         {
                                             FontSize = 15,
                                             HorizontalAlignment = HorizontalAlignment.Right,
                                             Margin = new Thickness(5, 0, 5, 0),
                                             VerticalAlignment = VerticalAlignment.Bottom,
                                             VerticalContentAlignment = VerticalAlignment.Bottom,
                                             IsEnabled = !Properties.Settings.Default.EvaluateFormulaOnKeyUp
                                         };
            manualFormulaTextBox.KeyUp += (o, e) =>
                                              {
                                                  if (!Properties.Settings.Default.EvaluateFormulaOnKeyUp)
                                                      return;
                                                  bool validFormula;
                                                  if (string.IsNullOrWhiteSpace(manualFormulaTextBox.Text))
                                                  {
                                                      validFormula = false;
                                                  }
                                                  else
                                                  {
                                                      formula = _evaluator.CompileFormula(manualFormulaTextBox.Text);
                                                      validFormula = formula.IsValid;
                                                  }

                                                  manualFormulaTextBox.Background = !validFormula && Properties.Settings.Default.EvaluateFormulaOnKeyUp ? new SolidColorBrush(Color.FromArgb(126, 255, 69, 0)) : new SolidColorBrush(Colors.White);
                                                  applyFormulaButton.IsEnabled = validFormula;
                                              };
            Grid.SetRow(manualFormulaTextBox, 1);
            manualTabGrid.Children.Add(manualFormulaTextBox);

            var buttonsWrapper = new WrapPanel
                                     {
                                         HorizontalAlignment = HorizontalAlignment.Right,
                                         Margin = new Thickness(0, 5, 0, 0),
                                     };
            Grid.SetRow(buttonsWrapper, 2);
            manualTabGrid.Children.Add(buttonsWrapper);

            var helpButton = new Button
                                 {
                                     FontSize = 15,
                                     HorizontalAlignment = HorizontalAlignment.Left,
                                     Margin = new Thickness(5, 0, 5, 0),
                                     VerticalAlignment = VerticalAlignment.Bottom,
                                     VerticalContentAlignment = VerticalAlignment.Bottom,
                                 };
            helpButton.Click += (o, e) =>
                                    {
                                        if (!CurrentDataSetNotNull)
                                            return;

                                        var message =
                                            "The program applies the formula entered across all sensors data points within the specified range.\n" +
                                           "The following gives an indication of the operations and syntax.\n\n" +
                                           "Mathematical operations\t [ -, +, *, ^, % ]\n" +
                                           "Mathematical functions\t [ Sin(y), Cos(y), Tan(y), Pi ]\n\n" +
                                           "Examples:\n" +
                                           "'x = x + 1'\n" +
                                           "'x = time.Date'\n" +
                                           "'x = x * Cos(x + 1) + 2'";

                                        if (Sensors.Count > 1)
                                            message =
                                               "The program applies the formula entered across all sensors data points within the specified range.\n" +
                                               "The following gives an indication of the operations and syntax.\n\n" +
                                               "Mathematical operations\t [ -, +, *, ^, % ]\n" +
                                               "Mathematical functions\t [ Sin(y), Cos(y), Tan(y), Pi ]\n\n" +
                                               "To set a data points value for a particular sensor, use that sensors variable followed by a space and an equals sign, then by the value.\n" +
                                               "   eg: To set the values of the sensor " + Sensors[0].Name + " to 5 for all points, use '" + Sensors[0].Variable.VariableName + " = 5' \n\n" +
                                               "To use a sensors values in a calculation, use that sesnors variable.\n" +
                                               "   eg: To make all the values of the sensor " + Sensors[0].Name + " equal to " + Sensors[1].Name +
                                                   ", use " + Sensors[0].Variable.VariableName + " = " + Sensors[1].Variable.VariableName + "\n\n" +
                                               "To use the data points time stamp in calculations use 'time.' followed by the time part desired.\n" +
                                               "   eg: time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second\n\n" +
                                               "Examples:\n" +
                                               "'x = x + 1'\n" +
                                               "'x = time.Date'\n" +
                                               "'x = x * Cos(x + 1) + 2'";
                                        Common.ShowMessageBox("Formula Help", message, false, false);
                                    };
            buttonsWrapper.Children.Add(helpButton);

            var helpButtonStackPanel = new StackPanel
                                           {
                                               Orientation = Orientation.Horizontal
                                           };
            helpButton.Content = helpButtonStackPanel;
            helpButtonStackPanel.Children.Add(new Image
                                                  {
                                                      Width = 32,
                                                      Height = 32,
                                                      Source = new BitmapImage(new Uri("pack://application:,,,/Images/help_32.png", UriKind.Absolute))
                                                  });
            helpButtonStackPanel.Children.Add(new TextBlock
                                                  {
                                                      Text = "Help",
                                                      VerticalAlignment = VerticalAlignment.Center,
                                                      Margin = new Thickness(5)
                                                  });

            applyFormulaButton.Click += (sender, eventArgs) =>
                                            {
                                                var validFormula = false;
                                                if (!string.IsNullOrWhiteSpace(manualFormulaTextBox.Text))
                                                {
                                                    formula = _evaluator.CompileFormula(manualFormulaTextBox.Text);
                                                    validFormula = formula.IsValid;
                                                }

                                                if (validFormula)
                                                {
                                                    var useSelected = Selection != null;
                                                    //if (Selection != null)
                                                    //    useSelected = Common.Confirm("Should we use your selection?",
                                                    //                                 "Should we use the date range of your selection for to apply the formula on?");
                                                    var skipMissingValues = false;
                                                    var detector = new MissingValuesDetector();

                                                    //Detect if missing values
                                                    var missingSensors = formula.SensorsUsed.Where(sensorVariable => detector.GetDetectedValues(sensorVariable.Sensor).Count > 0).Aggregate("", (current, sensorVariable) => current + ("\t" + sensorVariable.Sensor.Name + " (" + sensorVariable.VariableName + ")\n"));

                                                    if (missingSensors != "")
                                                    {
                                                        var specify =
                                                            (SpecifyValueViewModel)_container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel");
                                                        specify.Title = "Missing Values Detected";
                                                        specify.Message =
                                                            "The following sensors you have used in the formula contain missing values:\n\n" + missingSensors + "\nPlease select an action to take.";
                                                        specify.ShowComboBox = true;
                                                        specify.ShowCancel = true;
                                                        specify.CanEditComboBox = false;
                                                        specify.ComboBoxItems =
                                                            new List<string>(new[] { "Treat all missing values as zero", "Skip over all missing values" });
                                                        specify.ComboBoxSelectedIndex = 1;

                                                        _windowManager.ShowDialog(specify);

                                                        if (specify.WasCanceled) return;
                                                        skipMissingValues = specify.ComboBoxSelectedIndex == 1;
                                                    }

                                                    var reason = Common.RequestReason(_container, _windowManager, 12);

                                                    ApplicationCursor = Cursors.Wait;
                                                    if (useSelected)
                                                        _evaluator.EvaluateFormula(formula, Selection.LowerX.Round(TimeSpan.FromMinutes(CurrentDataset.DataInterval)), Selection.UpperX, skipMissingValues, reason);
                                                    else
                                                        _evaluator.EvaluateFormula(formula, StartTime.Round(TimeSpan.FromMinutes(CurrentDataset.DataInterval)), EndTime, skipMissingValues, reason);

                                                    ApplicationCursor = Cursors.Arrow;

                                                    Common.ShowMessageBox("Formula applied", "The formula was successfully applied to the sensor(s) involved.",
                                                                          false, false);
                                                    var sensorsUsed = formula.SensorsUsed.Select(x => x.Sensor);
                                                    foreach (var sensor in sensorsUsed)
                                                    {
                                                        Debug.Print("The sensor {0} was used", sensor.Name);
                                                    }
                                                    foreach (var graphableSensor in GraphableSensors.Where(x => sensorsUsed.Contains(x.Sensor)))
                                                    {
                                                        graphableSensor.RefreshDataPoints();
                                                        Debug.Print("The sensor {0} points were updated", graphableSensor.Sensor.Name);
                                                    }
                                                    UpdateGraph(false);
                                                    UpdateUndoRedo();
                                                }
                                                else
                                                {
                                                    var errorString = "";

                                                    if (formula != null && formula.CompilerResults.Errors.Count > 0)
                                                        errorString = formula.CompilerResults.Errors.Cast<CompilerError>().Aggregate(errorString, (current, error) => current + (error.ErrorText + "\n"));

                                                    Common.ShowMessageBoxWithExpansion("Unable to Apply Formula",
                                                                                       "An error was encounted when trying to apply the formula.\nPlease check the formula syntax.",
                                                                                       false, true, errorString);
                                                }
                                            };
            buttonsWrapper.Children.Add(applyFormulaButton);

            var applyFormulaButtonStackPanel = new StackPanel
                                                    {
                                                        Orientation = Orientation.Horizontal
                                                    };
            applyFormulaButton.Content = applyFormulaButtonStackPanel;
            applyFormulaButtonStackPanel.Children.Add(new Image
                                                        {
                                                            Width = 32,
                                                            Height = 32,
                                                            Source = new BitmapImage(new Uri("pack://application:,,,/Images/right_32.png", UriKind.Absolute))
                                                        });
            applyFormulaButtonStackPanel.Children.Add(new TextBlock
                                                        {
                                                            Text = "Apply",
                                                            VerticalAlignment = VerticalAlignment.Center,
                                                            Margin = new Thickness(5)
                                                        });

            var clearButton = new Button
                                {
                                    FontSize = 15,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    Margin = new Thickness(5, 0, 5, 0),
                                    VerticalAlignment = VerticalAlignment.Bottom,
                                    VerticalContentAlignment = VerticalAlignment.Bottom
                                };
            clearButton.Click += (o, e) =>
                                     {
                                         manualFormulaTextBox.Text = "";
                                         applyFormulaButton.IsEnabled = !Properties.Settings.Default.EvaluateFormulaOnKeyUp;
                                     };
            buttonsWrapper.Children.Add(clearButton);

            var clearButtonStackPanel = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal
                                            };
            clearButton.Content = clearButtonStackPanel;
            clearButtonStackPanel.Children.Add(new Image
                                                {
                                                    Width = 32,
                                                    Height = 32,
                                                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/delete_32.png", UriKind.Absolute))
                                                });
            clearButtonStackPanel.Children.Add(new TextBlock
                                                    {
                                                        Text = "Clear",
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        Margin = new Thickness(5)
                                                    });

            #endregion

            #region Automatic Tab

            var autoApplyButton = new Button
                                      {
                                          FontSize = 15,
                                          HorizontalAlignment = HorizontalAlignment.Right,
                                          Margin = new Thickness(5, 0, 5, 0),
                                          VerticalAlignment = VerticalAlignment.Bottom,
                                          VerticalContentAlignment = VerticalAlignment.Bottom,
                                          IsEnabled = false
                                      };

            var autoPreviewButton = new Button
                                        {
                                            FontSize = 15,
                                            HorizontalAlignment = HorizontalAlignment.Right,
                                            Margin = new Thickness(5, 0, 5, 0),
                                            VerticalAlignment = VerticalAlignment.Bottom,
                                            VerticalContentAlignment = VerticalAlignment.Bottom,
                                            IsEnabled = false
                                        };

            var automaticTabItem = new TabItem
                                       {
                                           Header = "Automatic"
                                       };
            manualAutoTabControl.Items.Add(automaticTabItem);

            var automaticGrid = new Grid();
            automaticTabItem.Content = automaticGrid;

            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var applyToStackPanel = new StackPanel
                                        {
                                            Orientation = Orientation.Horizontal
                                        };
            Grid.SetRow(applyToStackPanel, 0);
            automaticGrid.Children.Add(applyToStackPanel);

            applyToStackPanel.Children.Add(new TextBlock
                                               {
                                                   Text = "Apply To:",
                                                   Margin = new Thickness(0, 0, 15, 0),
                                                   VerticalAlignment = VerticalAlignment.Center
                                               });

            var applyToCombo = new ComboBox
                                   {
                                       Width = 120
                                   };
            applyToStackPanel.Children.Add(applyToCombo);

            applyToCombo.Items.Add("All");
            SensorsToCheckMethodsAgainst.CollectionChanged += (o, e) => applyToCombo.Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new ThreadStart(() =>
                                    {
                                        applyToCombo.Items.Clear();
                                        applyToCombo.Items.Add("All");
                                        SensorsForEditing.ForEach(x =>
                                                                  applyToCombo.Items.Add(x.Name));
                                        applyToCombo.SelectedIndex = 0;
                                    }));


            var automaticTextBlock = new TextBlock
                                         {
                                             Text = "Enter the calibration values below:",
                                             Margin = new Thickness(0, 5, 0, 5)
                                         };
            Grid.SetRow(automaticTextBlock, 1);
            automaticGrid.Children.Add(automaticTextBlock);

            var automaticValuesGrid = new Grid
                                          {
                                              Margin = new Thickness(5)
                                          };
            Grid.SetRow(automaticValuesGrid, 2);
            automaticGrid.Children.Add(automaticValuesGrid);

            automaticValuesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            automaticValuesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            automaticValuesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });

            automaticValuesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            automaticValuesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            automaticValuesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var calibratedTextBlock = new TextBlock
                                          {
                                              Text = "Start of Period",
                                              VerticalAlignment = VerticalAlignment.Center,
                                              HorizontalAlignment = HorizontalAlignment.Center
                                          };
            Grid.SetRow(calibratedTextBlock, 0);
            Grid.SetColumn(calibratedTextBlock, 1);
            automaticValuesGrid.Children.Add(calibratedTextBlock);

            var currentTextBlock = new TextBlock
                                       {
                                           Text = "End of Period",
                                           VerticalAlignment = VerticalAlignment.Center,
                                           HorizontalAlignment = HorizontalAlignment.Center
                                       };
            Grid.SetRow(currentTextBlock, 0);
            Grid.SetColumn(currentTextBlock, 2);
            automaticValuesGrid.Children.Add(currentTextBlock);

            var aTextBlock = new TextBlock
                                 {
                                     Text = "Span (High)",
                                     VerticalAlignment = VerticalAlignment.Center,
                                     HorizontalAlignment = HorizontalAlignment.Center
                                 };
            Grid.SetRow(aTextBlock, 2);
            Grid.SetColumn(aTextBlock, 0);
            automaticValuesGrid.Children.Add(aTextBlock);

            var bTextBlock = new TextBlock
                                 {
                                     Text = "Offset (Low)",
                                     VerticalAlignment = VerticalAlignment.Center,
                                     HorizontalAlignment = HorizontalAlignment.Center
                                 };
            Grid.SetRow(bTextBlock, 1);
            Grid.SetColumn(bTextBlock, 0);
            automaticValuesGrid.Children.Add(bTextBlock);

            var calibratedAValue = 0d;
            var calibratedAValid = false;
            var calibratedBValue = 0d;
            var calibratedBValid = false;
            var currentAValue = 0d;
            var currentAValid = false;
            var currentBValue = 0d;
            var currentBValid = false;

            var calibratedATextBox = new TextBox
                                       {
                                           VerticalAlignment = VerticalAlignment.Center,
                                           Margin = new Thickness(2),
                                           TabIndex = 1
                                       };
            calibratedATextBox.KeyUp += (o, e) =>
                                            {
                                                calibratedAValid = double.TryParse(calibratedATextBox.Text, out calibratedAValue);
                                                calibratedATextBox.Background = calibratedAValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                                // ReSharper disable AccessToModifiedClosure
                                                autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                                autoPreviewButton.IsEnabled = autoApplyButton.IsEnabled;
                                                // ReSharper restore AccessToModifiedClosure
                                            };

            Grid.SetRow(calibratedATextBox, 2);
            Grid.SetColumn(calibratedATextBox, 1);
            automaticValuesGrid.Children.Add(calibratedATextBox);

            var calibratedBTextBox = new TextBox
                                         {
                                             VerticalAlignment = VerticalAlignment.Center,
                                             Margin = new Thickness(2),
                                             TabIndex = 0
                                         };
            calibratedBTextBox.KeyUp += (o, e) =>
                                            {
                                                calibratedBValid = double.TryParse(calibratedBTextBox.Text, out calibratedBValue);
                                                calibratedBTextBox.Background = calibratedBValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                                // ReSharper disable AccessToModifiedClosure
                                                autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                                autoPreviewButton.IsEnabled = autoApplyButton.IsEnabled;
                                                // ReSharper restore AccessToModifiedClosure
                                            };
            Grid.SetRow(calibratedBTextBox, 1);
            Grid.SetColumn(calibratedBTextBox, 1);
            automaticValuesGrid.Children.Add(calibratedBTextBox);

            var currentATextBox = new TextBox
                                         {
                                             VerticalAlignment = VerticalAlignment.Center,
                                             Margin = new Thickness(2),
                                             TabIndex = 3
                                         };
            currentATextBox.KeyUp += (o, e) =>
                                         {
                                             currentAValid = double.TryParse(currentATextBox.Text, out currentAValue);
                                             currentATextBox.Background = currentAValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                             // ReSharper disable AccessToModifiedClosure
                                             autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                             autoPreviewButton.IsEnabled = autoApplyButton.IsEnabled;
                                             // ReSharper restore AccessToModifiedClosure
                                         };
            Grid.SetRow(currentATextBox, 2);
            Grid.SetColumn(currentATextBox, 2);
            automaticValuesGrid.Children.Add(currentATextBox);

            var currentBTextBox = new TextBox
                                         {
                                             VerticalAlignment = VerticalAlignment.Center,
                                             Margin = new Thickness(2),
                                             TabIndex = 2
                                         };
            currentBTextBox.KeyUp += (o, e) =>
                                         {
                                             currentBValid = double.TryParse(currentBTextBox.Text, out currentBValue);
                                             currentBTextBox.Background = currentBValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                             // ReSharper disable AccessToModifiedClosure
                                             autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                             autoPreviewButton.IsEnabled = autoApplyButton.IsEnabled;
                                             // ReSharper restore AccessToModifiedClosure
                                         };
            Grid.SetRow(currentBTextBox, 1);
            Grid.SetColumn(currentBTextBox, 2);
            automaticValuesGrid.Children.Add(currentBTextBox);

            var autoButtonsWrapPanel = new WrapPanel
                                           {
                                               Orientation = Orientation.Horizontal,
                                               HorizontalAlignment = HorizontalAlignment.Right
                                           };
            Grid.SetRow(autoButtonsWrapPanel, 4);
            automaticGrid.Children.Add(autoButtonsWrapPanel);


            autoApplyButton.Click += (o, e) =>
                                         {
                                             var useSelected = Selection != null;
                                             //if (Selection != null)
                                             //    useSelected = Common.Confirm("Should we use your selection?",
                                             //                                 "Should we use the date range of your selection for to apply the formula on?");
                                             var reason = Common.RequestReason(_container, _windowManager, 4);
                                             var successfulSensors = new List<Sensor>();
                                             foreach (var sensor in SensorsToCheckMethodsAgainst.Where(x => (string)applyToCombo.SelectedItem == "All" || (string)applyToCombo.SelectedItem == x.Name))
                                             {
                                                 try
                                                 {
                                                     sensor.AddState(useSelected
                                                                         ? sensor.CurrentState.Calibrate(
                                                                             Selection.LowerX, Selection.UpperX,
                                                                             calibratedAValue, calibratedBValue,
                                                                             currentAValue, currentBValue, reason)
                                                                         : sensor.CurrentState.Calibrate(StartTime,
                                                                                                         EndTime,
                                                                                                         calibratedAValue,
                                                                                                         calibratedBValue,
                                                                                                         currentAValue,
                                                                                                         currentBValue, reason));
                                                     sensor.CurrentState.Reason = reason;
                                                     successfulSensors.Add(sensor);
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     Common.ShowMessageBox("An Error Occured", ex.Message, false, true);
                                                 }
                                             }

                                             foreach (var graphableSensor in GraphableSensors.Where(x => successfulSensors.Contains(x.Sensor)))
                                                 graphableSensor.RefreshDataPoints();
                                             SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "AutoCalibration");
                                             UpdateUndoRedo();
                                         };
            autoButtonsWrapPanel.Children.Add(autoApplyButton);

            var autoApplyButtonStackPanel = new StackPanel
                                                {
                                                    Orientation = Orientation.Horizontal
                                                };
            autoApplyButton.Content = autoApplyButtonStackPanel;
            autoApplyButtonStackPanel.Children.Add(new Image
                                                       {
                                                           Width = 32,
                                                           Height = 32,
                                                           Source =
                                                               new BitmapImage(
                                                               new Uri("pack://application:,,,/Images/right_32.png",
                                                                       UriKind.Absolute))
                                                       });
            autoApplyButtonStackPanel.Children.Add(new TextBlock
                                                       {
                                                           Text = "Apply",
                                                           VerticalAlignment = VerticalAlignment.Center,
                                                           Margin = new Thickness(5)
                                                       });

            autoButtonsWrapPanel.Children.Add(autoPreviewButton);

            var autoPreviewButtonStackPanel = new StackPanel
                                                  {
                                                      Orientation = Orientation.Horizontal
                                                  };
            autoPreviewButton.Content = autoPreviewButtonStackPanel;
            autoPreviewButtonStackPanel.Children.Add(new Image
                                                         {
                                                             Width = 32,
                                                             Height = 32,
                                                             Source =
                                                                 new BitmapImage(
                                                                 new Uri("pack://application:,,,/Images/preview_32.png",
                                                                         UriKind.Absolute))
                                                         });
            var previewTextBlock = new TextBlock
                                       {
                                           Text = "Preview",
                                           VerticalAlignment = VerticalAlignment.Center,
                                           Margin = new Thickness(5)
                                       };
            autoPreviewButtonStackPanel.Children.Add(previewTextBlock);

            autoPreviewButton.Click += (o, e) =>
                                           {
                                               if (String.CompareOrdinal(previewTextBlock.Text, "Preview") == 0)
                                               {
                                                   var useSelected = Selection != null;
                                                   //if (Selection != null)
                                                   //    useSelected = Common.Confirm("Should we use your selection?",
                                                   //                                 "Should we use the date range of your selection for to apply the formula on?");
                                                   foreach (var sensor in SensorsToCheckMethodsAgainst.Where(x => (string)applyToCombo.SelectedItem == "All" || (string)applyToCombo.SelectedItem == x.Name))
                                                   {
                                                       var gSensor =
                                                           _sensorsToGraph.FirstOrDefault(x => x.Sensor == sensor);
                                                       if (gSensor == null)
                                                           continue;
                                                       try
                                                       {
                                                           gSensor.GeneratePreview(useSelected
                                                                                       ? sensor.CurrentState.Calibrate(
                                                                                           Selection.LowerX,
                                                                                           Selection.UpperX,
                                                                                           calibratedAValue,
                                                                                           calibratedBValue,
                                                                                           currentAValue, currentBValue,
                                                                                           new ChangeReason(-1,
                                                                                                            "Preview"))
                                                                                       : sensor.CurrentState.Calibrate(
                                                                                           StartTime,
                                                                                           EndTime,
                                                                                           calibratedAValue,
                                                                                           calibratedBValue,
                                                                                           currentAValue,
                                                                                           currentBValue,
                                                                                           new ChangeReason(-1,
                                                                                                            "Preview")));
                                                       }
                                                       catch (Exception ex)
                                                       {
                                                           Common.ShowMessageBox("An Error Occured", ex.Message, false,
                                                                                 true);
                                                       }
                                                   }
                                                   previewTextBlock.Text = "Reject";
                                               }
                                               else
                                               {
                                                   foreach (var gSensor in _sensorsToGraph)
                                                   {
                                                       gSensor.RemovePreview();
                                                   }
                                                   previewTextBlock.Text = "Preview";
                                               }
                                               SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "AutoCalibratePreview");
                                           };

            var autoClearButton = new Button
                                  {
                                      FontSize = 15,
                                      HorizontalAlignment = HorizontalAlignment.Right,
                                      Margin = new Thickness(5, 0, 5, 0),
                                      VerticalAlignment = VerticalAlignment.Bottom,
                                      VerticalContentAlignment = VerticalAlignment.Bottom
                                  };
            autoClearButton.Click += (o, e) =>
                                         {
                                             calibratedATextBox.Text = "";
                                             calibratedBTextBox.Text = "";
                                             currentATextBox.Text = "";
                                             currentBTextBox.Text = "";
                                             previewTextBlock.Text = "Preview";
                                             autoApplyButton.IsEnabled = false;
                                             autoPreviewButton.IsEnabled = false;
                                             var previewMade = GraphableSensors.FirstOrDefault(x => x.PreviewDataPoints != null) != null;
                                             if (previewMade)
                                             {
                                                 GraphableSensors.ForEach(x => x.RemovePreview());
                                                 SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "PreviewResetFromAutoClear");
                                             }
                                         };
            autoButtonsWrapPanel.Children.Add(autoClearButton);

            var autoClearButtonStackPanel = new StackPanel
                                            {
                                                Orientation = Orientation.Horizontal
                                            };
            autoClearButton.Content = autoClearButtonStackPanel;
            autoClearButtonStackPanel.Children.Add(new Image
                                                   {
                                                       Width = 32,
                                                       Height = 32,
                                                       Source =
                                                           new BitmapImage(
                                                           new Uri("pack://application:,,,/Images/delete_32.png",
                                                                   UriKind.Absolute))
                                                   });
            autoClearButtonStackPanel.Children.Add(new TextBlock
                                                   {
                                                       Text = "Clear",
                                                       VerticalAlignment = VerticalAlignment.Center,
                                                       Margin = new Thickness(5)
                                                   });

            #endregion

            return tabItem;
        }

        private void CheckTheseMethods(IEnumerable<IDetectionMethod> methodsToCheck)
        {
            if (!_detectionMethodsEnabled)
                return;

            var bw = new BackgroundWorker();

            methodsToCheck = methodsToCheck.ToList();

            foreach (var detectionMethod in methodsToCheck)
            {
                Debug.Print("[CheckTheseMethods] Clearing listbox for {0}", detectionMethod.Name);
                detectionMethod.ListBox.Items.Clear();
            }

            bw.DoWork += (o, e) =>
            {
                var valuesDictionary = new Dictionary<IDetectionMethod, IEnumerable<ErroneousValue>>();
                foreach (var detectionMethod in methodsToCheck)
                {
                    foreach (var sensor in SensorsToCheckMethodsAgainst)
                    {
                        WaitEventString = string.Format("Checking {0} for {1}", sensor.Name, detectionMethod.Name);
                        Debug.Print("[CheckTheseMethods] Checking {0} for {1}", sensor.Name, detectionMethod.Name);
                        var values =
                            detectionMethod.GetDetectedValues(sensor).Where(
                                x => x.TimeStamp >= StartTime && x.TimeStamp <= EndTime);
                        if (valuesDictionary.ContainsKey(detectionMethod))
                        {
                            var list = valuesDictionary[detectionMethod].ToList();
                            list.AddRange(values);
                            valuesDictionary[detectionMethod] = list;
                        }
                        valuesDictionary[detectionMethod] = values;
                    }
                }
                e.Result = valuesDictionary;
            };

            bw.RunWorkerCompleted += (o, e) =>
            {
                Debug.Print("Processing gained values");
                var dict = (Dictionary<IDetectionMethod, IEnumerable<ErroneousValue>>)e.Result;

                foreach (var pair in dict)
                {
                    Debug.Print("There are {0} values for the {1} listbox", pair.Value.Count(), pair.Key.Name);
                    foreach (var erroneousValue in pair.Value)
                    {
                        pair.Key.ListBox.Items.Add(erroneousValue);
                    }
                }

                Debug.Print("Finised processing list boxes");

                EnableFeatures();
                ShowProgressArea = false;
            };

            ShowProgressArea = true;
            ProgressIndeterminate = true;
            DisableFeatures();
            bw.RunWorkerAsync();
        }

        private void CheckTheseMethodsForThisSensor(IEnumerable<IDetectionMethod> methodsToCheck, Sensor sensor)
        {
            if (!_detectionMethodsEnabled)
                return;

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
                             {
                                 var valuesDictionary = new Dictionary<IDetectionMethod, IEnumerable<ErroneousValue>>();
                                 foreach (var detectionMethod in methodsToCheck)
                                 {
                                     WaitEventString = string.Format("Checking {0} for {1}", sensor.Name, detectionMethod.Name);
                                     var values =
                                         detectionMethod.GetDetectedValues(sensor).Where(
                                             x => x.TimeStamp >= StartTime && x.TimeStamp <= EndTime);
                                     valuesDictionary[detectionMethod] = values;

                                 }
                                 e.Result = valuesDictionary;
                             };

            bw.RunWorkerCompleted += (o, e) =>
            {
                Debug.Print("Processing gained values");
                var dict = (Dictionary<IDetectionMethod, IEnumerable<ErroneousValue>>)e.Result;

                foreach (var pair in dict)
                {
                    foreach (var erroneousValue in pair.Value)
                    {
                        pair.Key.ListBox.Items.Add(erroneousValue);
                    }
                }

                Debug.Print("Finised processing list boxes");

                EnableFeatures();
                ShowProgressArea = false;
            };

            ShowProgressArea = true;
            ProgressIndeterminate = true;
            DisableFeatures();
            bw.RunWorkerAsync();
        }

        private void UpdateDetectionMethodGraphableSensors()
        {
            var availableOptions =
                _sensorsToGraph.Where(x => SensorsToCheckMethodsAgainst.Contains(x.Sensor)).Select(x => x.Sensor).ToArray();
            foreach (var detectionMethod in _detectionMethods)
            {
                detectionMethod.SensorOptions = availableOptions;
            }
        }

        private void Interpolate(IEnumerable<ErroneousValue> values, IDetectionMethod methodCheckedAgainst)
        {
            values = values.ToList();
            var usingSelection = false;
            if (_graphEnabled && Selection != null && (!values.Any() || Common.Confirm("Should we use the values you've selected", "For this interpolation should we use the all the values in the range you've selected instead of those selected from the list of detected values")))
            {
                var list = new List<ErroneousValue>();
                if (ApplyToAllSensors && !Common.Confirm("Are you sure?",
                                   "You've set to apply changes to all sensors.\r\nThis means you're editing values you can't see"))
                    return;
                foreach (var sensor in ApplyToAllSensors ? Sensors : SensorsToCheckMethodsAgainst.ToList())
                {
                    var sensorCopy = sensor;
                    list.AddRange(sensor.CurrentState.Values.Where(
                        x =>
                        x.Key >= Selection.LowerX && x.Key <= Selection.UpperX && (_selectionBehaviour.UseFullYAxis || x.Value >= Selection.LowerY) &&
                        (_selectionBehaviour.UseFullYAxis || x.Value <= Selection.UpperY)).Select(x => new ErroneousValue(x.Key, sensorCopy)));
                }
                values = list;
                usingSelection = true;
            }

            if (!_graphEnabled && _erroneousValuesFromDataTable.Count > 0 && (!values.Any() || Common.Confirm("Should we use the values you've selected?", "For this interpolation should we use the values you've selected on the table instead of those selected from the list of detected values?")))
            {
                values = _erroneousValuesFromDataTable;
                usingSelection = true;
            }
            if (values.Count() < 0)
            {
                Common.ShowMessageBox("No values to interpolate", "You haven't given us any values to work with!", false, false);
                return;
            }

            var sensorList = values.Select(x => x.Owner).Distinct().ToList();

            var bw = new BackgroundWorker();

            var reason = Common.RequestReason(_container, _windowManager, (!usingSelection && methodCheckedAgainst != null) ? methodCheckedAgainst.DefaultReasonNumber : 7);

            bw.DoWork += (o, e) =>
                             {
                                 foreach (var sensor in sensorList)
                                 {
                                     sensor.AddState(sensor.CurrentState.Interpolate(values.Where(x => x.Owner == sensor).Select(x => x.TimeStamp).ToList(), sensor.Owner, reason));
                                 }
                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             FeaturesEnabled = true;
                                             ShowProgressArea = false;
                                             //Update the needed graphed items
                                             foreach (var graphableSensor in GraphableSensors.Where(x => sensorList.Contains(x.Sensor)))
                                                 graphableSensor.RefreshDataPoints();
                                             SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "Interpolate");
                                             UpdateUndoRedo();
                                             Common.ShowMessageBox("Values Updated", "The selected values were interpolated", false, false);
                                             CheckTheseMethods(new Collection<IDetectionMethod> { methodCheckedAgainst });
                                         };

            FeaturesEnabled = false;
            ShowProgressArea = true;
            ProgressIndeterminate = true;
            WaitEventString = "Interpolating values";
            bw.RunWorkerAsync();
        }

        private void RemoveValues(IEnumerable<ErroneousValue> values, IDetectionMethod methodCheckedAgainst)
        {
            values = values.ToList();
            var usingSelection = false;

            if (_graphEnabled && Selection != null && (!values.Any() || Common.Confirm("Should we use the values you've selected?", "To remove values should we use the all the values in the range you've selected instead of those selected from the list of detected values?")))
            {
                var list = new List<ErroneousValue>();
                if (ApplyToAllSensors && !Common.Confirm("Are you sure?",
                                   "You've set to apply changes to all sensors.\r\nThis means you're editing values you can't see"))
                    return;
                foreach (var sensor in ApplyToAllSensors ? Sensors : SensorsToCheckMethodsAgainst.ToList())
                {
                    var sensorCopy = sensor;
                    list.AddRange(sensor.CurrentState.Values.Where(
                        x =>
                        x.Key >= Selection.LowerX && x.Key <= Selection.UpperX && (_selectionBehaviour.UseFullYAxis || x.Value >= Selection.LowerY) &&
                        (_selectionBehaviour.UseFullYAxis || x.Value <= Selection.UpperY)).Select(x => new ErroneousValue(x.Key, sensorCopy)));
                }
                values = list;
                usingSelection = true;
            }

            if (!_graphEnabled && _erroneousValuesFromDataTable.Count > 0 && (!values.Any() || Common.Confirm("Should we use the values you've selected?", "When removing values should we use the values you've selected from the grid or those selected from the list of detected values?")))
            {
                values = _erroneousValuesFromDataTable;
                usingSelection = true;
            }


            if (values.Count() < 0)
            {
                Common.ShowMessageBox("No values to remove", "You haven't given us any values to work with!", false, false);
                return;
            }


            if (methodCheckedAgainst == _missingValuesDetector && !usingSelection)
            {
                Common.ShowMessageBox("Sorry, but that's a little hard",
                                      "We can't remove values that are already removed! Try another option", false,
                                      false);
                return;
            }

            var sensorList = values.Select(x => x.Owner).Distinct().ToList();

            var bw = new BackgroundWorker();
            var reason = Common.RequestReason(_container, _windowManager, (!usingSelection && methodCheckedAgainst != null) ? methodCheckedAgainst.DefaultReasonNumber : 7);
            bw.DoWork += (o, e) =>
            {
                foreach (var sensor in sensorList)
                {
                    sensor.AddState(sensor.CurrentState.RemoveValues(values.Where(x => x.Owner == sensor).Select(x => x.TimeStamp).ToList(), reason));
                }
            };

            bw.RunWorkerCompleted += (o, e) =>
            {
                FeaturesEnabled = true;
                ShowProgressArea = false;
                //Update the needed graphed items
                foreach (var graphableSensor in GraphableSensors.Where(x => sensorList.Contains(x.Sensor)))
                    graphableSensor.RefreshDataPoints();
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "RemoveValues");
                UpdateUndoRedo();
                Common.ShowMessageBox("Values Updated", "The selected values were removed", false, false);
                CheckTheseMethods(new Collection<IDetectionMethod> { methodCheckedAgainst });
            };

            FeaturesEnabled = false;
            ShowProgressArea = true;
            ProgressIndeterminate = true;
            WaitEventString = "Removing values";
            bw.RunWorkerAsync();
        }

        private void SpecifyValue(IEnumerable<ErroneousValue> values, IDetectionMethod methodCheckedAgainst)
        {
            values = values.ToList();
            var usingSelection = false;
            if (_graphEnabled && Selection != null && (!values.Any() || Common.Confirm("Should we use the values you've selected?", "Should we use the all the values in the range you've selected instead of those selected from the list of detected values?")))
            {
                var list = new List<ErroneousValue>();
                if (ApplyToAllSensors && !Common.Confirm("Are you sure?",
                                   "You've set to apply changes to all sensors.\r\nThis means you're editing values you can't see"))
                    return;
                foreach (var sensor in ApplyToAllSensors ? Sensors : SensorsToCheckMethodsAgainst.ToList())
                {
                    var sensorCopy = sensor;
                    list.AddRange(sensor.CurrentState.Values.Where(
                        x =>
                        x.Key >= Selection.LowerX && x.Key <= Selection.UpperX && (_selectionBehaviour.UseFullYAxis || x.Value >= Selection.LowerY) &&
                        (_selectionBehaviour.UseFullYAxis || x.Value <= Selection.UpperY)).Select(x => new ErroneousValue(x.Key, sensorCopy)));
                }
                values = list;
                usingSelection = true;
            }

            if (!_graphEnabled && _erroneousValuesFromDataTable.Count > 0 && (!values.Any() || Common.Confirm("Should we use the values you've selected?", "Should we use the values that you've selceted from the table instead of those selected from the list of detected values?")))
            {
                values = _erroneousValuesFromDataTable;
                usingSelection = true;
            }


            if (values.Count() < 0)
            {
                Common.ShowMessageBox("No values to set value to", "You haven't given us any values to work with!", false, false);
                return;
            }


            var sensorList = values.Select(x => x.Owner).Distinct().ToList();

            var specifyValueView = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;

            if (specifyValueView == null)
                return;

            float value;

            _windowManager.ShowDialog(specifyValueView);

            if (specifyValueView.WasCanceled)
                return;

            try
            {
                value = float.Parse(specifyValueView.Text);
            }
            catch (Exception)
            {
                Common.ShowMessageBox("An Error Occured", "Please enter a valid number.", true, true);
                return;
            }

            var reason = Common.RequestReason(_container, _windowManager, (!usingSelection && methodCheckedAgainst != null) ? methodCheckedAgainst.DefaultReasonNumber : 7);

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
            {
                foreach (var sensor in sensorList)
                {
                    sensor.AddState(sensor.CurrentState.MakeValue(values.Where(x => x.Owner == sensor).Select(x => x.TimeStamp).ToList(), value, reason));
                }
            };

            bw.RunWorkerCompleted += (o, e) =>
            {
                FeaturesEnabled = true;
                ShowProgressArea = false;
                //Update the needed graphed items
                foreach (var graphableSensor in GraphableSensors.Where(x => sensorList.Contains(x.Sensor)))
                    graphableSensor.RefreshDataPoints();
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "SpecifyValues");
                UpdateUndoRedo();
                Common.ShowMessageBox("Values Updated", "The selected values set to " + value, false, false);
                CheckTheseMethods(new Collection<IDetectionMethod> { methodCheckedAgainst });
            };

            FeaturesEnabled = false;
            ShowProgressArea = true;
            ProgressIndeterminate = true;
            WaitEventString = "Removing values";
            bw.RunWorkerAsync();
        }

        private void CalculateYAxis(bool resetRange = true)
        {
            var min = MinimumY(ChartSeries);
            var max = MaximumY(ChartSeries);

            if (Math.Abs(min - 0) < 0.01)
                min = -0.2;

            if (Math.Abs(max - 0) < 0.01)
                max = 0.2;

            if (min < double.MaxValue)
            {
                min = min - (Math.Abs(min * .2));
                max = max + (Math.Abs(max * .2));

                if (resetRange)
                    Range = new DoubleRange(min, max);
            }


            MinMinimum = (int)min;
            MaxMaximum = (int)max;

            MaxMinimum = (int)max;
            MinMaximum = (int)Math.Ceiling(min);
        }

        private void UpdateUndoRedo()
        {
            CanUndo = SensorsForEditing.FirstOrDefault(x => x.UndoStates.Count > 0) != null;
            CanRedo = SensorsForEditing.FirstOrDefault(x => x.RedoStates.Count > 0) != null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts a new import of data
        /// </summary>
        public void Import()
        {
            if (CurrentDataset == null)
            {
                Common.ShowMessageBox("No Current Site", "You need to select or create a site before you can import",
                                      false, false);
                return;
            }

            var bw = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

            var openFileDialog = new OpenFileDialog { Filter = @"CSV Files|*.csv" };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            bw.DoWork += (o, e) =>
                             {
                                 ShowProgressArea = true;
                                 ProgressIndeterminate = false;
                                 ProgressValue = 0;

                                 var reader = new CSVReader(openFileDialog.FileName);

                                 reader.ProgressChanged += (sender, args) =>
                                                               {
                                                                   ProgressValue = args.Progress;
                                                                   WaitEventString =
                                                                       string.Format("Importing from {0} {1}%",
                                                                                     openFileDialog.FileName,
                                                                                     ProgressValue);
                                                               };
                                 try
                                 {
                                     e.Result = reader.ReadSensors(bw, CurrentDataset);
                                 }
                                 catch (Exception ex)
                                 {
                                     Common.ShowMessageBoxWithException("Failed Import", "Bad File Format", false, true, ex);
                                     e.Result = null;
                                 }


                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             if (e.Result == null)
                                             {
                                                 ShowProgressArea = false;
                                                 EnableFeatures();
                                                 return;
                                             }


                                             var sensors = (List<Sensor>)e.Result;

                                             if (CurrentDataset.Sensors == null || CurrentDataset.Sensors.Count == 0)
                                                 CurrentDataset.Sensors = sensors;
                                             else
                                             {
                                                 var askUser =
                                                     _container.GetInstance(typeof(SpecifyValueViewModel),
                                                                            "SpecifyValueViewModel") as
                                                     SpecifyValueViewModel;

                                                 if (askUser == null)
                                                 {
                                                     Common.ShowMessageBox("EPIC FAIL", "RUN AROUND WITH NO REASON",
                                                                           false, true);
                                                     return;
                                                 }

                                                 askUser.ComboBoxItems = new List<string> { "Keep old values", "Keep new values" };
                                                 askUser.Text = "Keep old values";
                                                 askUser.ShowComboBox = true;
                                                 askUser.Message = "How do you want to handle overlapping points";
                                                 askUser.CanEditComboBox = false;
                                                 askUser.ComboBoxSelectedIndex = 0;
                                                 askUser.Title = "Importing";

                                                 _windowManager.ShowDialog(askUser);

                                                 var keepOldValues = askUser.ComboBoxSelectedIndex == 0;

                                                 var sensorMatchView =
                                                     _container.GetInstance(typeof(MatchToExistingSensorsViewModel),
                                                                            "MatchToExistingSensorsViewModel") as
                                                     MatchToExistingSensorsViewModel;

                                                 if (sensorMatchView == null)
                                                     return;

                                                 sensorMatchView.ExistingSensors = CurrentDataset.Sensors;
                                                 sensorMatchView.NewSensors = sensors;

                                                 _windowManager.ShowDialog(sensorMatchView);

                                                 foreach (var newSensor in sensors)
                                                 {
                                                     var match =
                                                         sensorMatchView.SensorLinks.FirstOrDefault(
                                                             x => x.MatchingSensor == newSensor);
                                                     if (match == null)
                                                     {
                                                         Debug.WriteLine("Adding new sensor");
                                                         CurrentDataset.Sensors.Add(newSensor);
                                                     }
                                                     else
                                                     {
                                                         var matchingSensor =
                                                             CurrentDataset.Sensors.FirstOrDefault(
                                                                 x => x == match.ExistingSensor);

                                                         if (matchingSensor == null)
                                                         {
                                                             Debug.WriteLine(
                                                                 "Failed to find the sensor again, embarrasing!");
                                                             continue;
                                                         }

                                                         Debug.WriteLine("Merging sensors");
                                                         //Otherwise clone the current state
                                                         var newState = matchingSensor.CurrentState.Clone();
                                                         //Check to see if values are inserted
                                                         var insertedValues = false;
                                                         var reason = ChangeReason.AddNewChangeReason("[Importer] Imported new values on " + DateTime.Now);
                                                         //And add values for any new dates we want
                                                         foreach (var value in newSensor.CurrentState.Values.Where(value =>
                                                                     !keepOldValues || !(matchingSensor.CurrentState.Values.ContainsKey(value.Key) || matchingSensor.RawData.Values.ContainsKey(value.Key))))
                                                         {
                                                             newState.Values[value.Key] = value.Value;
                                                             if (matchingSensor.CurrentState.Values.ContainsKey(value.Key) || matchingSensor.RawData.Values.ContainsKey(value.Key))
                                                                 newState.AddToChanges(value.Key, reason.ID);
                                                             matchingSensor.RawData.Values[value.Key] = value.Value;
                                                             insertedValues = true;
                                                         }

                                                         if (insertedValues)
                                                         {
                                                             //Give a reason
                                                             newState.Reason = reason;
                                                             //Insert new state
                                                             matchingSensor.AddState(newState);
                                                             matchingSensor.ClearUndoStates();
                                                             EventLogger.LogSensorInfo(CurrentDataset,
                                                                                       matchingSensor.Name,
                                                                                       "Added values from new import");
                                                         }
                                                     }
                                                 }
                                                 CurrentDataset.CalculateDataSetValues();
                                             }

                                             UpdateGUI();

                                             if (Sensors.FirstOrDefault(x => x.Variable == null) != null)
                                             {
                                                 var sensorVariables = SensorVariable.CreateSensorVariablesFromSensors(Sensors);
                                                 foreach (var sensor in Sensors)
                                                 {
                                                     sensor.Variable = sensorVariables.FirstOrDefault(x => x.Sensor == sensor);
                                                 }
                                             }
                                             _evaluator = new FormulaEvaluator(Sensors, CurrentDataset.DataInterval);
                                             _dateAnnotator.DataInterval = CurrentDataset.DataInterval;

                                             ShowProgressArea = false;
                                             EnableFeatures();
                                         };
            DisableFeatures();
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Starts a new export of data
        /// </summary>
        public void Export()
        {
            if (CurrentDataset == null)
            {
                Common.ShowMessageBox("No site selected", "To export you first need to have selected a site", false,
                                      false);
                return;
            }

            var exportWindow =
                    _container.GetInstance(typeof(ExportViewModel), "ExportViewModel") as ExportViewModel;
            if (exportWindow == null)
                return;
            exportWindow.Dataset = CurrentDataset;

            _windowManager.ShowDialog(exportWindow);
        }

        /// <summary>
        /// Saves to file
        /// </summary>
        public void Save()
        {
            if (CurrentDataset == null)
                return;

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
                             {
                                 WaitEventString = string.Format("Saving {0} to file", CurrentDataset.Site.Name);
                                 CurrentDataset.SaveToFile();
                             };
            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             ShowProgressArea = false;
                                             EnableFeatures();
                                         };
            ProgressIndeterminate = true;
            ShowProgressArea = true;
            DisableFeatures();
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Creates a new site
        /// </summary>
        public void CreateNewSite()
        {
            _sensorsToGraph.Clear();
            SensorsToCheckMethodsAgainst.Clear();
            UpdateGraph(true);

            var saveFirst = false;

            if (CurrentDataset != null)
            {
                saveFirst = Common.Confirm("Save before closing?",
                                           string.Format("Before we close '{0}' should we save it first?",
                                                         CurrentDataset.Site.Name));
            }

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
                             {
                                 ProgressIndeterminate = true;
                                 ShowProgressArea = true;
                                 if (!saveFirst)
                                     return;
                                 EventLogger.LogInfo(CurrentDataset, "Closing Save", "Saving to file before close");
                                 WaitEventString = string.Format("Saving {0} to file", CurrentDataset.Site.Name);
                                 CurrentDataset.SaveToFile();
                             };
            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             ShowProgressArea = false;
                                             EnableFeatures();
                                             var newDataset =
                                                 new Dataset(new Site(Site.NextID, "New Site", "", null, null,
                                                                      null));
                                             if (ShowSiteInformation(newDataset))
                                             {
                                                 CurrentDataset = newDataset;
                                             }
                                             else
                                             {
                                                 _graphableSensors = null;
                                                 NotifyOfPropertyChange(() => GraphableSensors);
                                             }
                                             NotifyOfPropertyChange(() => SiteNames);
                                         };

            DisableFeatures();
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Closes program
        /// </summary>
        public void Exit()
        {
            TryClose();
        }

        /// <summary>
        /// Update the selected site to the one corresponding to the selected index
        /// </summary>
        public void UpdateSelectedSite()
        {
            if (_chosenSelectedIndex == 0)
            {
                CreateNewSite();
                return;
            }

            if (!DataSetFiles.Any())
                CurrentDataset = null;

            if (CurrentDataset != null && DataSetFiles[_chosenSelectedIndex - 1] == CurrentDataset.SaveLocation)
                return;

            _sensorsToGraph.Clear();
            SensorsToCheckMethodsAgainst.Clear();
            UpdateGraph(true);

            if (_chosenSelectedIndex < 0)
            {
                CurrentDataset = null;
                return;
            }

            var saveFirst = false;

            if (CurrentDataset != null)
            {
                saveFirst = Common.Confirm("Save before closing?",
                                           string.Format("Before we close '{0}' should we save it first?",
                                                         CurrentDataset.Site.Name));
            }

            Debug.Print("Chosen Selected Index {0}", _chosenSelectedIndex);

            foreach (var file in DataSetFiles)
            {
                Debug.WriteLine(file);
            }

            Debug.Print("Chosen file is {0}", DataSetFiles[_chosenSelectedIndex - 1]);

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
            {
                ProgressIndeterminate = true;
                ShowProgressArea = true;

                if (saveFirst)
                {
                    EventLogger.LogInfo(CurrentDataset, "Closing Save", "Saving to file before close");
                    WaitEventString = string.Format("Saving {0} to file", CurrentDataset.Site.Name);
                    CurrentDataset.SaveToFile();
                }
                WaitEventString = string.Format("Loading from {0}", DataSetFiles[_chosenSelectedIndex - 1]);
                CurrentDataset = Dataset.LoadDataSet(DataSetFiles[_chosenSelectedIndex - 1]);
                EventLogger.LogInfo(null, "Loaded dataset", string.Format("Loaded {0}", DataSetFiles[_chosenSelectedIndex - 1]));
            };
            bw.RunWorkerCompleted += (o, e) =>
            {
                ShowProgressArea = false;
                Title = string.Format("B3: {0}", CurrentDataset.Site.Name);
                EnableFeatures();
            };

            DisableFeatures();
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Updates the Graph
        /// </summary>
        public void UpdateGraph(bool recalculateDateRange)
        {
            Debug.WriteLine("Updating Graph");
            ChartTitle = (_sensorsToGraph.Count > 0) ? string.Format("{0} [{1}m]", _sensorsToGraph[0].Sensor.Name, _sensorsToGraph[0].Sensor.Depth) : String.Empty;

            for (var i = 1; i < _sensorsToGraph.Count; i++)
                ChartTitle += string.Format(" and {0} [{1}m]", _sensorsToGraph[i].Sensor.Name, _sensorsToGraph[i].Sensor.Depth);

            var distinctUnits = (from sensor in _sensorsToGraph select sensor.Sensor.Unit).Distinct().ToArray();

            YAxisTitle = distinctUnits.Any() ? distinctUnits[0] : String.Empty;
            for (var i = 1; i < distinctUnits.Length; i++)
                YAxisTitle += string.Format(" and {0}", distinctUnits[i]);
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "UpdateGraph");
            CalculateYAxis();
            if (recalculateDateRange)
                CalculateGraphedEndPoints();
            NotifyOfPropertyChange(() => CanEditDates);
        }

        public void ShowLog()
        {
            var logWindow = (LogWindowViewModel)_container.GetInstance(typeof(LogWindowViewModel), "LogWindowViewModel");
            _windowManager.ShowWindow(logWindow);
        }

        public void Undo()
        {
            var orderedSensors = SensorsForEditing.OrderBy(x =>
            {
                var state = x.UndoStates.DefaultIfEmpty(new SensorState(x, DateTime.MaxValue)).FirstOrDefault();
                return state != null ? state.EditTimestamp : new DateTime();
            });

            var firstSensor = orderedSensors.FirstOrDefault();
            if (firstSensor == null) return;

            firstSensor.Undo();

            var graphToUpdate = GraphableSensors.FirstOrDefault(x => x.Sensor == firstSensor);
            if (graphToUpdate != null)
                graphToUpdate.RefreshDataPoints();

            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "Undo");
            CalculateYAxis();
            Common.ShowMessageBox("Undo suceeded", "Sucessfully stepped back the following sensors: \n\r\n\r" + firstSensor.Name, false, false);
            UpdateUndoRedo();
        }

        public void Redo()
        {
            var orderedSensors = SensorsForEditing.OrderBy(x =>
            {
                var state = x.RedoStates.DefaultIfEmpty(new SensorState(x, DateTime.MaxValue)).FirstOrDefault();
                return state != null ? state.EditTimestamp : new DateTime();
            });

            var firstSensor = orderedSensors.FirstOrDefault();
            if (firstSensor == null) return;

            firstSensor.Redo();

            var graphToUpdate = GraphableSensors.FirstOrDefault(x => x.Sensor == firstSensor);
            if (graphToUpdate != null)
                graphToUpdate.RefreshDataPoints();

            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "Redo");
            CalculateYAxis();
            Common.ShowMessageBox("Redo suceeded", "Sucessfully stepped forward the following sensors: \n\r\n\r" + firstSensor.Name, false, false);
            UpdateUndoRedo();
        }

        public void Interpolate()
        {
            var detectionMethod = _detectionMethods.FirstOrDefault(x => x.IsEnabled);

            Interpolate(detectionMethod != null ? detectionMethod.ListBox.SelectedItems.Cast<ErroneousValue>() : new List<ErroneousValue>(), detectionMethod);
        }

        public void RemoveValues()
        {
            var detectionMethod = _detectionMethods.FirstOrDefault(x => x.IsEnabled);

            RemoveValues(detectionMethod != null ? detectionMethod.ListBox.SelectedItems.Cast<ErroneousValue>() : new List<ErroneousValue>(), detectionMethod);
        }

        public void SpecifyValue()
        {
            var detectionMethod = _detectionMethods.FirstOrDefault(x => x.IsEnabled);

            SpecifyValue(detectionMethod != null ? detectionMethod.ListBox.SelectedItems.Cast<ErroneousValue>() : new List<ErroneousValue>(), detectionMethod);
        }

        public void EnableDetectionMethods()
        {
            _detectionMethodsEnabled = true;
            _selectedMethod.IsEnabled = true;
            CheckTheseMethods(_detectionMethods.Where(x => x.IsEnabled));
        }

        public void DisableDetectionMethods()
        {
            _detectionMethodsEnabled = false;
            _selectedMethod.IsEnabled = false;
        }

        public void RevertToRaw()
        {
            if (!CurrentDataSetNotNull || Sensors == null || Sensors.Count == 0)
                return;

            var onlyGraphed = false;

            if (_sensorsToGraph.Count > 0)
            {
                var specify = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
                if (specify == null)
                    return;
                specify.CanEditComboBox = false;
                specify.ShowComboBox = true;
                specify.Title = "What are we reverting to raw?";
                specify.Message = _sensorsToGraph.Aggregate("You are currently graphing these sensors:\r\n\n",
                                          (current, sensor) => current + string.Format("\r\n{0}", sensor.Sensor.Name));
                specify.Message +=
                    "\r\n\n Would you like to only revert to raw the sensors currently graphed or all your sensors?\r\n\nNOTE: THIS WILL REVERT ALL CHANGES AND CANNOT BE UNDONE";
                specify.ShowCancel = true;

                specify.ComboBoxItems = new List<string> { "Only the graphed sensors", "All sensors" };
                specify.ComboBoxSelectedIndex = 0;

                _windowManager.ShowDialog(specify);

                if (specify.WasCanceled)
                    return;

                onlyGraphed = specify.ComboBoxSelectedIndex == 0;
            }
            else
            {
                if (!Common.Confirm("Reverting to raw", "Are you sure you want to revert all sensors to their raw values?\r\n\nNOTE: THIS WILL REVERT ALL CHANGES AND CANNOT BE UNDONE"))
                    return;
            }

            if (onlyGraphed)
                SensorsForEditing.ForEach(x => x.RevertToRaw());
            else
                Sensors.ForEach(x => x.RevertToRaw());

            GraphableSensors.ForEach(x => x.RefreshDataPoints());

            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "RevertedToRaw");
        }

        public void RevertGraphedToRaw()
        {
            if (_sensorsToGraph.Count == 0)
                return;

            var useSelection = _selection != null &&
                               Common.Confirm("Use selection?",
                                              "Should we only revert what is currently selected on the graph?\r\n(Otherwise we will revert all that is visible on the graph)");

            foreach (var sensor in SensorsToCheckMethodsAgainst)
            {
                if (useSelection)
                {
                    if (_selectionBehaviour.UseFullYAxis)
                        sensor.RevertToRaw(Selection.LowerX, Selection.UpperX);
                    else
                        sensor.RevertToRaw(Selection.LowerX, Selection.UpperX, (float)Selection.LowerY, (float)Selection.UpperY);
                }
                else
                    sensor.RevertToRaw(StartTime, EndTime);
            }
            foreach (var graphableSensor in _sensorsToGraph)
            {
                graphableSensor.RefreshDataPoints();
            }
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "RevertSelectionToRaw");
        }

        public void ShowCalibrationDetails(Sensor sensor)
        {
            var view = _container.GetInstance(typeof(CalibrationDetailsViewModel), "CalibrationDetailsViewModel") as CalibrationDetailsViewModel;

            if (view == null) return;

            view.Sensor = sensor;
            _windowManager.ShowWindow(view);
        }

        public void ShowGraphHelp()
        {
            Common.ShowMessageBox("About Graphing", "Select a sensor(s) using the check-boxes in the sensor metadata list, to display data on the graph.\r\n\nYou may choose to display all available data points on the graph or, to increase speed, limit the number of points displayed. If the number of points exceeds the display limit, the graph background will turn pink to alert you that there may be hidden data within the date range. You can click-drag on the graph to zoom in on a range of data. Hold down shift to select a range of data without zooming, in order to modify only the selected range using the tools to the left. Raw data may be superimposed at any time by selecting ‘Graph raw data’ and you can revert to the raw data at any time (this will revert only the range of data currently displayed on the graph). You can manually adjust the date range and y-axis, or select ‘Show annotations’ to display the data values for the timestamp on which the cursor is located.\r\nSelections are reset on a single mouse click and zoom out is a double mouse click.", false, false);
        }

        public void EnableActions()
        {
            ActionsEnabled = _previousActionsStatus;
        }

        public void DisableActions()
        {
            _previousActionsStatus = ActionsEnabled;
            ActionsEnabled = false;
        }

        public void ShowHeatMap()
        {
            var view = _container.GetInstance(typeof(HeatMapViewModel), "HeatMapViewModel") as HeatMapViewModel;
            view.AvailableSensors = Sensors;
            _windowManager.ShowWindow(view);
        }

        #endregion

        #region Event Handlers

        public void ClosingRequested(CancelEventArgs eventArgs)
        {
            if (!FeaturesEnabled)
            {
                eventArgs.Cancel = true;
                return;
            }

            if (CurrentDataset != null && Common.Confirm("Save?", string.Format("Do you want to save {0} before closing?", CurrentDataset.Site.Name)))
            {
                WaitEventString = string.Format("Saving {0} to file", CurrentDataset.Site.Name);
                ProgressIndeterminate = true;
                ShowProgressArea = true;
                CurrentDataset.SaveToFile();
            }

            Debug.WriteLine("Closing Program");
        }

        public void AddToGraph(GraphableSensor graphableSensor, bool updateGraph = true)
        {
            if (_sensorsToGraph.FirstOrDefault(x => x.BoundsSet) != null)
                graphableSensor.SetUpperAndLowerBounds(StartTime, EndTime);
            _sensorsToGraph.Add(graphableSensor);
            Debug.Print("{0} was added to the graph list", graphableSensor.Sensor);
            DisableFeatures();
            UpdateGraph(_sensorsToGraph.Count < 2);
            //UpdateDetectionMethodGraphableSensors();
            EnableFeatures();
            AddToEditingSensors(graphableSensor);
            UpdateUndoRedo();
            NotifyOfPropertyChange(() => UncheckAllSensors);
        }

        public void AddToGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            AddToGraph(graphableSensor);
        }

        public void RemoveFromGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            _selectionBehaviour.ResetSelection();
            if (_sensorsToGraph.Contains(graphableSensor))
                _sensorsToGraph.Remove(graphableSensor);
            Debug.Print("{0} was removed from the graph list", graphableSensor.Sensor);
            DisableFeatures();
            UpdateGraph(false);
            UpdateDetectionMethodGraphableSensors();
            EnableFeatures();
            RemoveFromEditingSensors(eventArgs);
            UpdateUndoRedo();
            NotifyOfPropertyChange(() => UncheckAllSensors);
        }

        public void ShowCurrentSiteInformation()
        {
            ShowSiteInformation(CurrentDataset);
        }

        /// <summary>
        /// Fired when the start date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void StartTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null || (DateTime)e.NewValue == StartTime)
                return;

            if ((e.OldValue != null && (DateTime)e.OldValue == new DateTime()) || (DateTime)e.NewValue < EndTime)
                StartTime = (DateTime)e.NewValue;
            else if (e.OldValue != null)
                StartTime = (DateTime)e.OldValue;

            foreach (var sensor in _sensorsToGraph)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }

            if (e.OldValue != null && (DateTime)e.OldValue != DateTime.MinValue)
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "StartTimeChanged");
        }

        /// <summary>
        /// Fired when the end date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null || (DateTime)e.NewValue == EndTime)
                return;

            if ((e.OldValue != null && (DateTime)e.OldValue == new DateTime()) || (DateTime)e.NewValue > StartTime)
                EndTime = (DateTime)e.NewValue;
            else if (e.OldValue != null)
                EndTime = (DateTime)e.OldValue;

            foreach (var sensor in _sensorsToGraph)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }

            if (e.OldValue != null && (DateTime)e.OldValue != DateTime.MaxValue)
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "EndTimeChanged");
        }

        public void ColourChanged(RoutedPropertyChangedEventArgs<Color> args, GraphableSensor owner)
        {
            if (ChartSeries == null)
                return;

            var matchingLineSeries = ChartSeries.FirstOrDefault(x =>
                                                                    {
                                                                        var dataPoints = x.DataSeries as DataSeries<DateTime, float>;
                                                                        return dataPoints != null && dataPoints.Title == owner.Sensor.Name;
                                                                    });

            if (matchingLineSeries == null)
                return;

            Debug.Print("Matched to graphed line series {0} attempting to update", matchingLineSeries.Name);

            matchingLineSeries.LineStroke = new SolidColorBrush(args.NewValue);

            NotifyOfPropertyChange(() => ChartSeries);
        }

        public void DeleteSensor(GraphableSensor gSensor)
        {
            if (!Common.Confirm("Are you sure?", string.Format("Do you really want to delete {0}?", gSensor.Sensor.Name)))
                return;
            CurrentDataset.Sensors.Remove(gSensor.Sensor);
            if (_sensorsToGraph.Contains(gSensor))
                _sensorsToGraph.Remove(gSensor);
            if (SensorsToCheckMethodsAgainst.Contains(gSensor.Sensor))
                SensorsToCheckMethodsAgainst.Remove(gSensor.Sensor);
            _graphableSensors = null;
            NotifyOfPropertyChange(() => GraphableSensors);
        }

        public void ExportChart(Chart chart)
        {
            if (_sensorsToGraph.Count == 0)
            {
                Common.ShowMessageBox("No Graph Showing",
                                      "You haven't selected a sensor to graph so there is nothing to export!", false,
                                      false);
                return;
            }

            var exportView = (_container.GetInstance(typeof(ExportToImageViewModel), "ExportToImageViewModel") as ExportToImageViewModel);
            if (exportView == null)
            {
                EventLogger.LogError(null, "Image Exporter", "Failed to get a export image view");
                return;
            }

            //Set up the view
            exportView.Chart = chart;
            exportView.SelectedSensors = _sensorsToGraph.ToArray();

            //Show the dialog
            _windowManager.ShowDialog(exportView);
        }

        public void AddToEditingSensors(GraphableSensor graphableSensor)
        {
            SensorsToCheckMethodsAgainst.Add(graphableSensor.Sensor);
            graphableSensor.Sensor.PropertyChanged += SensorPropertyChanged;
            UpdateDetectionMethodGraphableSensors();
            NotifyOfPropertyChange(() => SensorsForEditing);
            CheckTheseMethodsForThisSensor(_detectionMethods.Where(x => x.IsEnabled), graphableSensor.Sensor);
            UpdateDataTable();
        }

        public void AddToEditingSensors(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            AddToEditingSensors(graphableSensor);
        }

        public void RemoveFromEditingSensors(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (SensorsToCheckMethodsAgainst.Contains(graphableSensor.Sensor))
                SensorsToCheckMethodsAgainst.Remove(graphableSensor.Sensor);
            graphableSensor.Sensor.PropertyChanged -= SensorPropertyChanged;
            NotifyOfPropertyChange(() => SensorsForEditing);
            UpdateDetectionMethodGraphableSensors();
            UpdateDataTable();

            foreach (var detectionMethod in _detectionMethods.Where(x => x.IsEnabled))
            {
                Debug.Print("{0} is enabled checking to remove values", detectionMethod.Name);

                var itemsToRemove =
                    detectionMethod.ListBox.Items.Cast<ErroneousValue>().Where(
                        value => !SensorsToCheckMethodsAgainst.Contains(value.Owner)).ToList();

                foreach (var erroneousValue in itemsToRemove)
                {
                    detectionMethod.ListBox.Items.Remove(erroneousValue);
                }
                detectionMethod.ListBox.Items.Refresh();
            }
        }

        public void DetectionMethodChanged(SelectionChangedEventArgs eventArgs)
        {
            GraphableSensors.ForEach(x => x.RemovePreview());

            if (eventArgs.RemovedItems.Count > 0)
            {
                var oldTabItem = eventArgs.RemovedItems[0] as TabItem;
                if (oldTabItem != null)
                {
                    var oldTabItemHeader = oldTabItem.Header as TextBlock;
                    var oldDetectionMethod = _detectionMethods.FirstOrDefault(x => oldTabItemHeader != null && x.Abbreviation == oldTabItemHeader.Text);
                    if (oldDetectionMethod != null)
                    {
                        Debug.Print("Turning off: {0}", oldDetectionMethod.Name);
                        oldDetectionMethod.IsEnabled = false;
                        oldDetectionMethod.SettingsGrid.IsEnabled = false;
                        oldDetectionMethod.ListBox.Items.Clear();
                        oldDetectionMethod.ListBox.IsEnabled = false;
                        SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "DetectionMethodChanged");
                    }
                    else
                        SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "ForcePreviewReset");
                    _selectedMethod = null;
                }
            }

            if (eventArgs.AddedItems.Count > 0)
            {
                var newTabItem = eventArgs.AddedItems[0] as TabItem;
                if (newTabItem != null)
                {
                    var newTabItemHeader = newTabItem.Header as TextBlock;
                    _selectionBehaviour.UseFullYAxis = _useFullYAxis || (newTabItem.Header is string &&
                                                       (String.CompareOrdinal((newTabItem.Header as string), "Calibration") == 0 || String.CompareOrdinal((newTabItem.Header as string), "Automatic") == 0 || String.CompareOrdinal((newTabItem.Header as string), "Manual") == 0));
                    NotInCalibrationMode = !(newTabItem.Header is string &&
                                             (String.CompareOrdinal((newTabItem.Header as string), "Calibration") == 0 ||
                                              String.CompareOrdinal((newTabItem.Header as string), "Automatic") == 0 ||
                                              String.CompareOrdinal((newTabItem.Header as string), "Manual") == 0));
                    var newDetectionMethod = _detectionMethods.FirstOrDefault(x => newTabItemHeader != null && x.Abbreviation == newTabItemHeader.Text);
                    if (newDetectionMethod != null)
                    {
                        Debug.Print("Turning on: {0}", newDetectionMethod.Name);
                        newDetectionMethod.IsEnabled = true;
                        newDetectionMethod.SettingsGrid.IsEnabled = true;
                        newDetectionMethod.ListBox.IsEnabled = true;
                        CheckTheseMethods(new Collection<IDetectionMethod> { newDetectionMethod });
                    }
                    _selectedMethod = newDetectionMethod;
                }
            }
        }

        public void KeyDown(KeyEventArgs eventArgs)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (eventArgs.Key)
                {
                    case Key.S:
                        Save();
                        break;
                    case Key.Z:
                        if (CanUndo)
                            Undo();
                        break;
                    case Key.Y:
                        if (CanRedo)
                            Redo();
                        break;
                }
            }
            else if (ActionsEnabled && eventArgs.Key == Key.Delete)
            {
                RemoveValues();
            }

        }

        private void SensorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_detectionMethodsEnabled) return;

            var enabledDetector = _detectionMethods.FirstOrDefault(x => x.IsEnabled);
            if (enabledDetector != null)
            {
                if (enabledDetector is MinMaxDetector && (e.PropertyName == "UpperLimit" || e.PropertyName == "LowerLimit"))
                    CheckTheseMethods(new[] { enabledDetector });
                else if (enabledDetector is ToHighRateOfChangeDetector && (e.PropertyName == "MaxRateOfChange"))
                    CheckTheseMethods(new[] { enabledDetector });
            }
        }

        public void AddNewMetaData(GraphableSensor sensor, KeyEventArgs keyPressEvent, ComboBox sender)
        {
            if (keyPressEvent.Key != Key.Enter)
                return;

            Debug.Print("Enter key pressed creating new meta data");
            Debug.Print("Sender {0} Sensor {1}", sender, sensor);

            var newMetaData = new SensorMetaData(sender.Text);
            sensor.Sensor.MetaData.Add(newMetaData);
            sensor.Sensor.CurrentMetaData = newMetaData;
        }

        public void EnableGraph()
        {
            _graphEnabled = true;
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph, "EnablingGraph");
        }

        public void DisableGraph()
        {
            SampleValues(0, new Collection<GraphableSensor>(), "DisablingGraph");
            _graphEnabled = false;
            UpdateDataTable();
        }

        public void DeleteRequestedFromDataTable(RoutedEventArgs eventArgs)
        {
            RemoveValues();
        }

        public void SelectedCellsChanged(SelectedCellsChangedEventArgs eventArgs)
        {
            foreach (var cell in eventArgs.AddedCells.Where(x => x.Column != null && x.Column.DisplayIndex != 0))
            {
                var row = cell.Item as DataRowView;
                if (row == null || Sensors.Count(x => String.CompareOrdinal(x.Name.Replace(".", ""), (string)cell.Column.Header) == 0) != 1) continue;

                var timeStamp = (DateTime)row.Row[0];
                var sensor = Sensors.FirstOrDefault(x => String.CompareOrdinal(x.Name.Replace(".", ""), (string)cell.Column.Header) == 0);

                if (sensor == null) continue;

                _erroneousValuesFromDataTable.Add(new ErroneousValue(timeStamp, sensor));
            }

            foreach (var cell in eventArgs.RemovedCells.Where(x => x.Column != null && x.Column.DisplayIndex != 0))
            {
                var row = cell.Item as DataRowView;
                if (row == null || Sensors.Count(x => String.CompareOrdinal(x.Name.Replace(".", ""), (string)cell.Column.Header) == 0) != 1) continue;

                var timeStamp = (DateTime)row.Row[0];
                var sensor = Sensors.FirstOrDefault(x => String.CompareOrdinal(x.Name.Replace(".", ""), (string)cell.Column.Header) == 0);

                if (sensor == null) continue;

                _erroneousValuesFromDataTable.Remove(new ErroneousValue(timeStamp, sensor));
            }

            ActionsEnabled = _erroneousValuesFromDataTable.Count > 0;
        }

        #endregion
    }
}