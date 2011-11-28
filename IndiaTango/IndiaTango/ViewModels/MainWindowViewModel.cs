using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Caliburn.Micro;
using IndiaTango.Models;
using Microsoft.Windows.Controls;
using Visiblox.Charts;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using GroupBox = System.Windows.Controls.GroupBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;
using Path = System.IO.Path;
using SelectionMode = System.Windows.Controls.SelectionMode;
using Cursors = System.Windows.Input.Cursors;
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

            _sensorsToGraph = new List<GraphableSensor>();
            _sensorsToCheckMethodsAgainst = new List<Sensor>();

            #region Set Up Detection Methods

            _minMaxRateofChangeDetector = new MinMaxDetector();
            _minMaxRateofChangeDetector.GraphUpdateNeeded += () =>
                                                                 {
                                                                     SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
                                                                     CalculateYAxis(false);
                                                                 };

            _runningMeanStandardDeviationDetector = new RunningMeanStandardDeviationDetector();
            _runningMeanStandardDeviationDetector.GraphUpdateNeeded += () => SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);

            _runningMeanStandardDeviationDetector.RefreshDetectedValues +=
                () => CheckTheseMethods(new Collection<IDetectionMethod> { _runningMeanStandardDeviationDetector });

            _missingValuesDetector = new MissingValuesDetector { IsEnabled = true };

            _detectionMethods = new List<IDetectionMethod> { _missingValuesDetector, _minMaxRateofChangeDetector, new ToHighRateOfChangeDetector(), _runningMeanStandardDeviationDetector };

            #endregion

            #region Set Up Behaviours

            var behaviourManager = new BehaviourManager { AllowMultipleEnabled = true };

            #region Zoom Behaviour
            _zoomBehaviour = new CustomZoomBehaviour { IsEnabled = true };
            _zoomBehaviour.ZoomRequested += (o, e) =>
            {
                _startTime = (DateTime)e.FirstPoint.X;
                NotifyOfPropertyChange(() => StartTime);
                EndTime = (DateTime)e.SecondPoint.X;
                foreach (var detectionMethod in _detectionMethods.Where(x => x.IsEnabled))
                {
                    var itemsToRemove =
                        detectionMethod.ListBox.Items.Cast<ErroneousValue>().Where(
                            x => x.TimeStamp < StartTime || x.TimeStamp > EndTime).ToList();

                    foreach (var erroneousValue in itemsToRemove)
                    {
                        detectionMethod.ListBox.Items.Remove(erroneousValue);
                    }
                }
            };
            _zoomBehaviour.ZoomResetRequested += o =>
            {
                foreach (var sensor in _sensorsToGraph)
                {
                    sensor.RemoveBounds();
                }
                CheckTheseMethods(_detectionMethods.Where(x => x.IsEnabled));
                CalculateGraphedEndPoints();
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
            };

            behaviourManager.Behaviours.Add(_zoomBehaviour);
            #endregion

            #region Background Behaviour
            _background = new Canvas { Visibility = Visibility.Collapsed };
            var backgroundBehaviour = new GraphBackgroundBehaviour(_background);
            behaviourManager.Behaviours.Add(backgroundBehaviour);
            #endregion

            #region Selection Behaviour

            _selectionBehaviour = new CustomSelectionBehaviour { IsEnabled = false };
            _selectionBehaviour.SelectionMade += (start, end) =>
                                                     {
                                                         //TODO: Let the things know
                                                     };

            _selectionBehaviour.SelectionReset += o =>
                                                      {
                                                          //TODO:
                                                      };
            behaviourManager.Behaviours.Add(_selectionBehaviour);
            #endregion

            Behaviour = behaviourManager;

            #endregion

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
        private readonly List<GraphableSensor> _sensorsToGraph;
        private readonly List<Sensor> _sensorsToCheckMethodsAgainst;
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
        private readonly MinMaxDetector _minMaxRateofChangeDetector;
        private readonly RunningMeanStandardDeviationDetector _runningMeanStandardDeviationDetector;
        private readonly MissingValuesDetector _missingValuesDetector;
        private List<GraphableSensor> _graphableSensors;
        private FormulaEvaluator _evaluator;
        private bool _canUndo;
        private bool _canRedo;
        private bool _showRaw;
        private readonly CustomZoomBehaviour _zoomBehaviour;
        private readonly CustomSelectionBehaviour _selectionBehaviour;
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
                }
                UpdateGUI();
                UpdateUndoRedo();
            }
        }

        private string[] DataSetFiles
        {
            get { return _dataSetFiles ?? (_dataSetFiles = Dataset.GetAllDataSetFileNames()); }
        }

        private List<Sensor> SensorsForEditing
        {
            get { return _sensorsToCheckMethodsAgainst; }
        }

        private bool CurrentDataSetNotNull
        {
            get { return CurrentDataset != null; }
        }

        private bool CanUndo
        {
            get { return _canUndo; }
            set
            {
                _canUndo = value;
                NotifyOfPropertyChange(() => CanUndo);
            }
        }

        private bool CanRedo
        {
            get { return _canRedo; }
            set
            {
                _canRedo = value;
                NotifyOfPropertyChange(() => CanRedo);
            }
        }

        private string[] SiteNamesNoSelectedIndexRefresh
        {
            get
            {
                var siteNamesList = DataSetFiles.Select(x => x.Substring(x.LastIndexOf('\\') + 1, x.Length - x.LastIndexOf('\\') - 4)).ToList();
                siteNamesList.Insert(0, "Create new site...");
                return siteNamesList.ToArray();
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
                if (CurrentDataset != null && siteNames.Contains(CurrentDataset.Site.Name))
                    ChosenSelectedIndex = Array.IndexOf(siteNames, CurrentDataset.Site.Name);
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
                if (!_waitText.Contains("Importing from"))
                    EventLogger.LogInfo(CurrentDataset, "Wait Event String", string.Format("Updated to {0}", _waitText));
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

                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
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
            }
        }

        public bool SelectionModeEnabled
        {
            get { return _selectionBehaviour.IsEnabled; }
            set
            {
                _selectionBehaviour.IsEnabled = value;
                _zoomBehaviour.IsEnabled = !value;
                if(!value)
                {
                    _selectionBehaviour.MouseLeftButtonDoubleClick(new Point());
                }
            }
        }

        #endregion

        #region Private Methods

        private void UpdateGUI()
        {
            NotifyOfPropertyChange(() => Sensors);
            _graphableSensors = null;
            NotifyOfPropertyChange(() => GraphableSensors);
        }

        private void SampleValues(int numberOfPoints, ICollection<GraphableSensor> sensors)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            var numberOfExtraLinesToTakeIntoConsiderationWhenSampling = 0;

            if (sensors.Count > 0)
                numberOfExtraLinesToTakeIntoConsiderationWhenSampling += _detectionMethods.Where(x => x.IsEnabled && x.HasGraphableSeries).Sum(detectionMethod => (from lineSeries in detectionMethod.GraphableSeries(sensors.ElementAt(0).Sensor, StartTime, EndTime) select lineSeries.DataSeries.Cast<DataPoint<DateTime, float>>().Count() into numberInLineSeries let numberInSensor = sensors.ElementAt(0).DataPoints.Count() select numberInLineSeries / (double)numberInSensor).Count(percentage => percentage > 0.2d));

            if (_showRaw)
                numberOfExtraLinesToTakeIntoConsiderationWhenSampling += sensors.Count;

            Debug.Print("There are {0} lines that have been counted as sensors for sampling", numberOfExtraLinesToTakeIntoConsiderationWhenSampling);

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count() / (numberOfPoints / (sensors.Count + numberOfExtraLinesToTakeIntoConsiderationWhenSampling));
                Debug.Print("[{3}] Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate, sensor.Sensor.Name);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
                if (_showRaw)
                {
                    var rawSeries = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name + "[RAW]", sensor.RawDataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.RawDataPoints);
                    generatedSeries.Add(new LineSeries { DataSeries = rawSeries, LineStroke = new SolidColorBrush(sensor.RawDataColour) });
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
                if (sensor.DataPoints.Count() <= 0) continue;

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
            _startTime = minimum;
            NotifyOfPropertyChange(() => StartTime);
            EndTime = maximum;
            Debug.WriteLine("As a result start {0} and end {1}", StartTime, EndTime);
        }

        private void ShowSiteInformation(Dataset dataSetToShow)
        {
            if (dataSetToShow == null)
            {
                Common.ShowMessageBox("No Site Selected",
                                      "To view site information you must first select or create a site", false, false);
                return;
            }

            var view = _container.GetInstance(typeof(EditSiteDataViewModel), "EditSiteDataViewModel") as EditSiteDataViewModel;

            if (view == null)
            {
                EventLogger.LogError(null, "Loading Site Editor", "Critical! Failed to get a View!!");
                return;
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
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var title = new TextBlock { Text = method.Name, FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(3), TextWrapping = TextWrapping.Wrap };

            Grid.SetRow(title, 0);

            tabItemGrid.Children.Add(title);

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

            Grid.SetRow(detectionMethodListBox, 2);

            tabItemGrid.Children.Add(detectionMethodListBox);

            var actions = new GroupBox { Header = "Actions", BorderBrush = Brushes.OrangeRed };

            var actionsStackPanelWrapper = new StackPanel();

            var undoRedoWrap = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };

            var undoButton = new SplitButton { FontSize = 15, Width = 155, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5), IsEnabled = CanUndo };
            undoButton.Click += (o, e) =>
                                    {
                                        Undo();
                                        UpdateUndoRedo();
                                    };
            PropertyChanged += (o, e) =>
                                   {
                                       if (e.PropertyName == "CanUndo")
                                       {
                                           undoButton.IsEnabled = CanUndo;
                                       }
                                   };
            var undoButtonStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            undoButtonStackPanel.Children.Add(new Image { Width = 32, Height = 32, Source = new BitmapImage(new Uri("pack://application:,,,/Images/cancel_32.png", UriKind.Absolute)) });
            undoButtonStackPanel.Children.Add(new TextBlock { Text = "Undo", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });

            undoButton.Content = undoButtonStackPanel;

            undoRedoWrap.Children.Add(undoButton);

            var redoButton = new SplitButton { FontSize = 15, Width = 155, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5), IsEnabled = CanRedo };
            redoButton.Click += (o, e) =>
                                    {
                                        Redo();
                                        UpdateUndoRedo();
                                    };
            PropertyChanged += (o, e) =>
                                   {
                                       if (e.PropertyName == "CanRedo")
                                       {
                                           redoButton.IsEnabled = CanRedo;
                                       }
                                   };
            var redoButtonStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            redoButtonStackPanel.Children.Add(new Image { Width = 32, Height = 32, Source = new BitmapImage(new Uri("pack://application:,,,/Images/redo_32.png", UriKind.Absolute)) });
            redoButtonStackPanel.Children.Add(new TextBlock { Text = "Redo", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });

            redoButton.Content = redoButtonStackPanel;

            undoRedoWrap.Children.Add(redoButton);

            actionsStackPanelWrapper.Children.Add(undoRedoWrap);

            actionsStackPanelWrapper.Children.Add(new Rectangle { Height = 3, Margin = new Thickness(0, 10, 0, 10), Fill = Brushes.OrangeRed, Stroke = Brushes.White, SnapsToDevicePixels = true });

            var dataEditingWrapper = new WrapPanel { Margin = new Thickness(5), IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Center };

            var interpolateButton = new Button
                                        {
                                            FontSize = 15,
                                            Width = 100,
                                            Height = 100,
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Right,
                                            Margin = new Thickness(5),
                                            VerticalContentAlignment = VerticalAlignment.Bottom
                                        };

            interpolateButton.Click += (o, e) => Interpolate(listBox.SelectedItems.Cast<ErroneousValue>(), method);

            var interpolateButtonStackPanel = new StackPanel();

            interpolateButtonStackPanel.Children.Add(new Image { Width = 64, Height = 64, Source = new BitmapImage(new Uri("pack://application:,,,/Images/graph_interpolate.png", UriKind.Absolute)) });
            interpolateButtonStackPanel.Children.Add(new TextBlock
                                                         {
                                                             Text = "Interpolate",
                                                             HorizontalAlignment = HorizontalAlignment.Center
                                                         });



            interpolateButton.Content = interpolateButtonStackPanel;

            dataEditingWrapper.Children.Add(interpolateButton);

            var deleteButton = new Button
            {
                FontSize = 15,
                Width = 100,
                Height = 100,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Bottom
            };

            var deleteButtonStackPanel = new StackPanel();

            deleteButtonStackPanel.Children.Add(new Image { Width = 64, Height = 64, Source = new BitmapImage(new Uri("pack://application:,,,/Images/remove_point.png", UriKind.Absolute)) });
            deleteButtonStackPanel.Children.Add(new TextBlock
            {
                Text = "Delete",
                HorizontalAlignment = HorizontalAlignment.Center
            });

            deleteButton.Content = deleteButtonStackPanel;

            deleteButton.Click += (o, e) => RemoveValues(listBox.SelectedItems.Cast<ErroneousValue>(), method);

            dataEditingWrapper.Children.Add(deleteButton);

            var specifyButton = new Button
            {
                FontSize = 15,
                Width = 100,
                Height = 100,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Bottom
            };

            var specifyButtonStackPanel = new StackPanel();

            specifyButtonStackPanel.Children.Add(new Image { Width = 64, Height = 64, Source = new BitmapImage(new Uri("pack://application:,,,/Images/graph_specify.png", UriKind.Absolute)) });
            specifyButtonStackPanel.Children.Add(new TextBlock
            {
                Text = "Specify Value",
                HorizontalAlignment = HorizontalAlignment.Center
            });

            specifyButton.Content = specifyButtonStackPanel;

            specifyButton.Click += (o, e) => SpecifyValue(listBox.SelectedItems.Cast<ErroneousValue>(), method);

            dataEditingWrapper.Children.Add(specifyButton);

            actionsStackPanelWrapper.Children.Add(dataEditingWrapper);

            actions.Content = actionsStackPanelWrapper;

            listBox.SelectionChanged += (o, e) =>
                                            {
                                                var box = o as ListBox;
                                                if (box != null)
                                                    dataEditingWrapper.IsEnabled = box.SelectedItems.Count > 0;
                                            };

            Grid.SetRow(actions, 3);

            tabItemGrid.Children.Add(actions);

            tabItem.Content = tabItemGrid;
            return tabItem;
        }

        private TabItem GenerateCalibrationTabItem()
        {
            var tabItem = new TabItem { Header = "Calibration", IsEnabled = FeaturesEnabled };
            //Build the Grid to base it all on and add it
            var tabItemGrid = new Grid();
            tabItem.Content = tabItemGrid;

            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); //Undo-Redo
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); //Formula area

            #region Undo Redo Building

            var undoRedoStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            Grid.SetRow(undoRedoStackPanel, 0);
            tabItemGrid.Children.Add(undoRedoStackPanel);

            var undoButton = new SplitButton { FontSize = 15, Width = 155, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5), IsEnabled = CanUndo };
            undoButton.Click += (o, e) =>
                                    {
                                        Undo();
                                        UpdateUndoRedo();
                                    };
            PropertyChanged += (o, e) =>
                                   {
                                       if (e.PropertyName == "CanUndo")
                                       {
                                           undoButton.IsEnabled = CanUndo;
                                       }
                                   };
            undoRedoStackPanel.Children.Add(undoButton);

            var undoButtonStackPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            undoButtonStackPanel.Children.Add(new Image { Width = 32, Height = 32, Source = new BitmapImage(new Uri("pack://application:,,,/Images/cancel_32.png", UriKind.Absolute)) });
            undoButtonStackPanel.Children.Add(new TextBlock { Text = "Undo", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });

            undoButton.Content = undoButtonStackPanel;

            var redoButton = new SplitButton { FontSize = 15, Width = 155, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5), IsEnabled = CanRedo };
            redoButton.Click += (o, e) =>
                                    {
                                        Redo();
                                        UpdateUndoRedo();
                                    };
            PropertyChanged += (o, e) =>
                                   {
                                       if (e.PropertyName == "CanRedo")
                                       {
                                           redoButton.IsEnabled = CanRedo;
                                       }
                                   };
            undoRedoStackPanel.Children.Add(redoButton);

            var redoButtonStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            redoButtonStackPanel.Children.Add(new Image { Width = 32, Height = 32, Source = new BitmapImage(new Uri("pack://application:,,,/Images/redo_32.png", UriKind.Absolute)) });
            redoButtonStackPanel.Children.Add(new TextBlock { Text = "Redo", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });

            redoButton.Content = redoButtonStackPanel;

            #endregion

            var contentGrid = new Grid();
            Grid.SetRow(contentGrid, 1);
            tabItemGrid.Children.Add(contentGrid);

            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var seperator = new Rectangle
                                {
                                    Height = 3,
                                    Margin = new Thickness(5),
                                    Fill = Brushes.OrangeRed,
                                    SnapsToDevicePixels = true,
                                    Stroke = Brushes.White
                                };
            Grid.SetRow(seperator, 0);
            contentGrid.Children.Add(seperator);

            var calibrationMethodStackPanel = new StackPanel { Margin = new Thickness(5), Orientation = Orientation.Horizontal };
            Grid.SetRow(calibrationMethodStackPanel, 1);
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
            Grid.SetRow(manualAutoTabControl, 2);
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
                                             IsEnabled = false
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
                                        if (!CurrentDataSetNotNull && Sensors.Count < 2)
                                            return;

                                        var message =
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

                                                    ApplicationCursor = Cursors.Wait;
                                                    _evaluator.EvaluateFormula(formula, StartTime, EndTime, skipMissingValues);

                                                    ApplicationCursor = Cursors.Arrow;

                                                    Common.RequestReason(SensorVariable.CreateSensorsFromSensorVariables(formula.SensorsUsed), _container, _windowManager, "Formula '" + manualFormulaTextBox.Text + "' successfully applied to the sensor.");

                                                    Common.ShowMessageBox("Formula applied", "The formula was successfully applied to the selected sensor.",
                                                                          false, false);
                                                    var sensorsUsed = formula.SensorsUsed.Select(x => x.Sensor);
                                                    foreach (var graphableSensor in GraphableSensors.Where(x => sensorsUsed.Contains(x.Sensor)))
                                                        graphableSensor.RefreshDataPoints();
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
                                         applyFormulaButton.IsEnabled = false;
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

            var automaticTabItem = new TabItem
                                       {
                                           Header = "Automatic"
                                       };
            manualAutoTabControl.Items.Add(automaticTabItem);

            var automaticGrid = new Grid();
            automaticTabItem.Content = automaticGrid;

            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            automaticGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var automaticTextBlock = new TextBlock
                                         {
                                             Text = "Enter the calibration values below:",
                                             Margin = new Thickness(0, 5, 0, 5)
                                         };
            Grid.SetRow(automaticTextBlock, 0);
            automaticGrid.Children.Add(automaticTextBlock);

            var automaticValuesGrid = new Grid
                                          {
                                              Margin = new Thickness(5)
                                          };
            Grid.SetRow(automaticValuesGrid, 1);
            automaticGrid.Children.Add(automaticValuesGrid);

            automaticValuesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            automaticValuesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
            automaticValuesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });

            automaticValuesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            automaticValuesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            automaticValuesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var calibratedTextBlock = new TextBlock
                                          {
                                              Text = "Calibrated",
                                              VerticalAlignment = VerticalAlignment.Center,
                                              HorizontalAlignment = HorizontalAlignment.Center
                                          };
            Grid.SetRow(calibratedTextBlock, 0);
            Grid.SetColumn(calibratedTextBlock, 1);
            automaticValuesGrid.Children.Add(calibratedTextBlock);

            var currentTextBlock = new TextBlock
                                       {
                                           Text = "Current",
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
            Grid.SetRow(aTextBlock, 1);
            Grid.SetColumn(aTextBlock, 0);
            automaticValuesGrid.Children.Add(aTextBlock);

            var bTextBlock = new TextBlock
                                 {
                                     Text = "Offset (Low)",
                                     VerticalAlignment = VerticalAlignment.Center,
                                     HorizontalAlignment = HorizontalAlignment.Center
                                 };
            Grid.SetRow(bTextBlock, 2);
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
                                           Margin = new Thickness(2)
                                       };
            calibratedATextBox.KeyUp += (o, e) =>
                                            {
                                                calibratedAValid = double.TryParse(calibratedATextBox.Text, out calibratedAValue);
                                                calibratedATextBox.Background = calibratedAValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                                // ReSharper disable AccessToModifiedClosure
                                                autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                                // ReSharper restore AccessToModifiedClosure
                                            };

            Grid.SetRow(calibratedATextBox, 1);
            Grid.SetColumn(calibratedATextBox, 1);
            automaticValuesGrid.Children.Add(calibratedATextBox);

            var calibratedBTextBox = new TextBox
                                         {
                                             VerticalAlignment = VerticalAlignment.Center,
                                             Margin = new Thickness(2)
                                         };
            calibratedBTextBox.KeyUp += (o, e) =>
                                            {
                                                calibratedBValid = double.TryParse(calibratedBTextBox.Text, out calibratedBValue);
                                                calibratedBTextBox.Background = calibratedBValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                                // ReSharper disable AccessToModifiedClosure
                                                autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                                // ReSharper restore AccessToModifiedClosure
                                            };
            Grid.SetRow(calibratedBTextBox, 2);
            Grid.SetColumn(calibratedBTextBox, 1);
            automaticValuesGrid.Children.Add(calibratedBTextBox);

            var currentATextBox = new TextBox
                                         {
                                             VerticalAlignment = VerticalAlignment.Center,
                                             Margin = new Thickness(2)
                                         };
            currentATextBox.KeyUp += (o, e) =>
                                         {
                                             currentAValid = double.TryParse(currentATextBox.Text, out currentAValue);
                                             currentATextBox.Background = currentAValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                             // ReSharper disable AccessToModifiedClosure
                                             autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                             // ReSharper restore AccessToModifiedClosure
                                         };
            Grid.SetRow(currentATextBox, 1);
            Grid.SetColumn(currentATextBox, 2);
            automaticValuesGrid.Children.Add(currentATextBox);

            var currentBTextBox = new TextBox
                                         {
                                             VerticalAlignment = VerticalAlignment.Center,
                                             Margin = new Thickness(2)
                                         };
            currentBTextBox.KeyUp += (o, e) =>
                                         {
                                             currentBValid = double.TryParse(currentBTextBox.Text, out currentBValue);
                                             currentBTextBox.Background = currentAValid ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
                                             // ReSharper disable AccessToModifiedClosure
                                             autoApplyButton.IsEnabled = calibratedAValid && calibratedBValid && currentAValid && currentBValid;
                                             // ReSharper restore AccessToModifiedClosure
                                         };
            Grid.SetRow(currentBTextBox, 2);
            Grid.SetColumn(currentBTextBox, 2);
            automaticValuesGrid.Children.Add(currentBTextBox);

            var autoButtonsWrapPanel = new WrapPanel
                                           {
                                               Orientation = Orientation.Horizontal,
                                               HorizontalAlignment = HorizontalAlignment.Right
                                           };
            Grid.SetRow(autoButtonsWrapPanel, 3);
            automaticGrid.Children.Add(autoButtonsWrapPanel);


            autoApplyButton.Click += (o, e) =>
                                         {
                                             var successfulSensors = new List<Sensor>();
                                             foreach (var sensor in _sensorsToCheckMethodsAgainst)
                                             {
                                                 try
                                                 {
                                                     sensor.AddState(sensor.CurrentState.Calibrate(StartTime, EndTime, calibratedAValue, calibratedBValue, currentAValue, currentBValue));
                                                     successfulSensors.Add(sensor);
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     Common.ShowMessageBox("An Error Occured", ex.Message, false, true);
                                                 }
                                             }
                                             Common.RequestReason(successfulSensors, _container, _windowManager, "Calibration CalA='" + calibratedAValue + "', CalB='" + calibratedBValue + "', CurA='" + currentAValue + "', CurB='" + currentBValue + "' successfully applied to the sensor.");

                                             foreach (var graphableSensor in GraphableSensors.Where(x => successfulSensors.Contains(x.Sensor)))
                                                 graphableSensor.RefreshDataPoints();
                                             SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
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
                                             autoApplyButton.IsEnabled = false;
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
                    foreach (var sensor in _sensorsToCheckMethodsAgainst)
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
                _sensorsToGraph.Where(x => _sensorsToCheckMethodsAgainst.Contains(x.Sensor)).Select(x => x.Sensor).ToArray();
            foreach (var detectionMethod in _detectionMethods)
            {
                detectionMethod.SensorOptions = availableOptions;
            }
        }

        private void Interpolate(IEnumerable<ErroneousValue> values, IDetectionMethod methodCheckedAgainst)
        {
            values = values.ToList();
            if (values.Count() < 0)
                return;

            if (!Common.Confirm("Are you sure?", "Are you sure you want to interpolate these values?"))
                return;

            var sensorList = values.Select(x => x.Owner).Distinct().ToList();

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
                             {
                                 foreach (var sensor in sensorList)
                                 {
                                     sensor.AddState(sensor.CurrentState.Interpolate(values.Where(x => x.Owner == sensor).Select(x => x.TimeStamp).ToList(), sensor.Owner));
                                 }
                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             FeaturesEnabled = true;
                                             ShowProgressArea = false;
                                             //Update the needed graphed items
                                             foreach (var graphableSensor in GraphableSensors.Where(x => sensorList.Contains(x.Sensor)))
                                                 graphableSensor.RefreshDataPoints();
                                             SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
                                             UpdateUndoRedo();
                                             Common.ShowMessageBox("Values Updated", "The selected values were interpolated", false, false);
                                             Common.RequestReason(sensorList, _container, _windowManager, "Values were interpolated");
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
            if (values.Count() < 0)
                return;

            if (methodCheckedAgainst == _missingValuesDetector)
            {
                Common.ShowMessageBox("Sorry, but that's a little hard",
                                      "We can't remove values that are already removed! Try another option", false,
                                      false);
                return;
            }

            if (!Common.Confirm("Are you sure?", "Are you sure you want to remove these values?"))
                return;

            var sensorList = values.Select(x => x.Owner).Distinct().ToList();

            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
            {
                foreach (var sensor in sensorList)
                {
                    sensor.AddState(sensor.CurrentState.RemoveValues(values.Where(x => x.Owner == sensor).Select(x => x.TimeStamp).ToList()));
                }
            };

            bw.RunWorkerCompleted += (o, e) =>
            {
                FeaturesEnabled = true;
                ShowProgressArea = false;
                //Update the needed graphed items
                foreach (var graphableSensor in GraphableSensors.Where(x => sensorList.Contains(x.Sensor)))
                    graphableSensor.RefreshDataPoints();
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
                UpdateUndoRedo();
                Common.ShowMessageBox("Values Updated", "The selected values were removed", false, false);
                Common.RequestReason(sensorList, _container, _windowManager, "Values were removed");
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
            if (values.Count() < 0)
                return;

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


            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
            {
                foreach (var sensor in sensorList)
                {
                    sensor.AddState(sensor.CurrentState.MakeValue(values.Where(x => x.Owner == sensor).Select(x => x.TimeStamp).ToList(), value));
                }
            };

            bw.RunWorkerCompleted += (o, e) =>
            {
                FeaturesEnabled = true;
                ShowProgressArea = false;
                //Update the needed graphed items
                foreach (var graphableSensor in GraphableSensors.Where(x => sensorList.Contains(x.Sensor)))
                    graphableSensor.RefreshDataPoints();
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
                UpdateUndoRedo();
                Common.ShowMessageBox("Values Updated", "The selected values set to " + value, false, false);
                Common.RequestReason(sensorList, _container, _windowManager, "Values were set to " + value);
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
                min = -1;

            if (Math.Abs(max - 0) < 0.01)
                max = 1;

            if (min < double.MaxValue)
            {
                min = min - (Math.Abs(min * .2));
                max = max + (Math.Abs(max * .2));

                if (_sensorsToGraph.Count < 2 && resetRange)
                    Range = new DoubleRange(min, max);
            }


            MinMinimum = (int)min;
            MaxMaximum = (int)max;

            MaxMinimum = (int)max;
            MinMaximum = (int)Math.Ceiling(min);
        }

        private void UpdateUndoRedo()
        {
            CanUndo = Sensors.FirstOrDefault(x => x.UndoStates.Count > 0) != null;
            CanRedo = Sensors.FirstOrDefault(x => x.RedoStates.Count > 0) != null;
        }

        private void Undo()
        {
            var orderedSensors = Sensors.OrderBy(x =>
                                                     {
                                                         var state = x.UndoStates.DefaultIfEmpty(new SensorState(x, DateTime.MaxValue)).FirstOrDefault();
                                                         return state != null ? state.EditTimestamp : new DateTime();
                                                     });

            var firstSensor = orderedSensors.FirstOrDefault();
            if (firstSensor == null) return;

            var sensorState = firstSensor.UndoStates.FirstOrDefault();
            if (sensorState == null) return;

            var timestamp = sensorState.EditTimestamp;
            var sensorsToUndo = orderedSensors.TakeWhile(x =>
                                                             {
                                                                 var firstOrDefault = x.UndoStates.FirstOrDefault();
                                                                 //TODO: Not the best
                                                                 return firstOrDefault != null && firstOrDefault.EditTimestamp - timestamp < new TimeSpan(0, 0, 2);
                                                             }).ToList();

            foreach (var sensor in sensorsToUndo)
            {
                sensor.Undo();
            }
            var message = sensorsToUndo.Aggregate("Sucessfully stepped back the following sensors: \n\r\n\r", (current, sensorVariable) =>
                                              current + string.Format("{0}\n\r", sensorVariable.Name));

            foreach (var graphableSensor in GraphableSensors.Where(x => sensorsToUndo.Contains(x.Sensor)))
                graphableSensor.RefreshDataPoints();
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
            Common.ShowMessageBox("Undo suceeded", message, false, false);
        }

        private void Redo()
        {
            var orderedSensors = Sensors.OrderBy(x =>
                                                     {
                                                         var state =
                                                             x.RedoStates.DefaultIfEmpty(new SensorState(x,
                                                                                                         DateTime.
                                                                                                             MaxValue)).
                                                                 FirstOrDefault();
                                                         return state != null ? state.EditTimestamp : new DateTime();
                                                     });

            var firstSensor = orderedSensors.FirstOrDefault();
            if (firstSensor == null) return;

            var sensorState = firstSensor.RedoStates.FirstOrDefault();
            if (sensorState == null) return;

            var timestamp = sensorState.EditTimestamp;
            var sensorsToRedo = orderedSensors.TakeWhile(x =>
                                                             {
                                                                 var firstOrDefault = x.RedoStates.FirstOrDefault();
                                                                 //TODO: Not the best
                                                                 return firstOrDefault != null &&
                                                                        firstOrDefault.EditTimestamp - timestamp <
                                                                        new TimeSpan(0, 0, 2);
                                                             }).ToList();

            foreach (var sensor in sensorsToRedo)
            {
                sensor.Redo();
            }
            var message = sensorsToRedo.Aggregate("Sucessfully stepped forward the following sensors: \n\r\n\r", (current, sensorVariable) =>
                                              current + string.Format("{0}\n\r", sensorVariable.Name));

            foreach (var graphableSensor in GraphableSensors.Where(x => sensorsToRedo.Contains(x.Sensor)))
                graphableSensor.RefreshDataPoints();
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
            Common.ShowMessageBox("Redo suceeded", message, false, false);
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
                                                 return;

                                             var sensors = (List<Sensor>)e.Result;

                                             if (CurrentDataset.Sensors.Count == 0)
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

                                                 foreach (var sensor in sensors)
                                                 {
                                                     var askUserDialog =
                                                         _container.GetInstance(typeof(SpecifyValueViewModel),
                                                                                "SpecifyValueViewModel") as
                                                         SpecifyValueViewModel;

                                                     if (askUserDialog == null)
                                                         return;

                                                     askUserDialog.Title = "What sensor does this belong to?";
                                                     askUserDialog.ComboBoxItems =
                                                         new List<string>(
                                                             (from x in CurrentDataset.Sensors select x.Name));
                                                     askUserDialog.ShowComboBox = true;
                                                     askUserDialog.ShowCancel = true;
                                                     askUserDialog.ComboBoxSelectedIndex = 0;
                                                     askUserDialog.CanEditComboBox = false;
                                                     askUserDialog.Message =
                                                         string.Format(
                                                             "Match {0} against an existing sensor. \n\r Cancel to create a new sensor",
                                                             sensor.Name);

                                                     _windowManager.ShowDialog(askUserDialog);

                                                     if (askUserDialog.WasCanceled)
                                                     {
                                                         Debug.WriteLine("Adding new sensor");
                                                         CurrentDataset.Sensors.Add(sensor);
                                                     }
                                                     else
                                                     {
                                                         var matchingSensor =
                                                             CurrentDataset.Sensors.Where(
                                                                 x =>
                                                                 x.Name.CompareTo(
                                                                     askUserDialog.ComboBoxItems[
                                                                         askUserDialog.ComboBoxSelectedIndex]) == 0)
                                                                 .
                                                                 DefaultIfEmpty(null).FirstOrDefault();

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

                                                         //And add values for any new dates we want
                                                         foreach (
                                                             var value in
                                                                 sensor.CurrentState.Values.Where(
                                                                     value =>
                                                                     !keepOldValues ||
                                                                     !newState.Values.ContainsKey(value.Key)))
                                                         {
                                                             newState.Values[value.Key] = value.Value;
                                                             insertedValues = true;
                                                         }
                                                         //Give a reason
                                                         newState.Reason = "Imported new values on " + DateTime.Now;
                                                         if (insertedValues)
                                                         {
                                                             //Insert new state
                                                             matchingSensor.AddState(newState);
                                                             EventLogger.LogSensorInfo(CurrentDataset,
                                                                                       matchingSensor.Name,
                                                                                       "Added values from new import");
                                                         }
                                                     }
                                                 }
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
            _sensorsToCheckMethodsAgainst.Clear();
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

                                             var newSitesName = "New Site";
                                             if (File.Exists(Path.Combine(Common.DatasetSaveLocation, "New Site.b3")))
                                             {
                                                 var x = 1;
                                                 while (File.Exists(Path.Combine(Common.DatasetSaveLocation, string.Format("New Site{0}.b3", x))))
                                                     x++;

                                                 newSitesName = "New Site" + x;
                                             }
                                             var newDataset = new Dataset(new Site(0, newSitesName, "", null, null, null, null));
                                             ShowSiteInformation(newDataset);
                                             CurrentDataset = newDataset;
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
            if (CurrentDataset != null && SiteNamesNoSelectedIndexRefresh[_chosenSelectedIndex] == CurrentDataset.Site.Name)
                return;

            if (_chosenSelectedIndex == 0)
            {
                CreateNewSite();
                return;
            }

            _sensorsToGraph.Clear();
            _sensorsToCheckMethodsAgainst.Clear();
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

            YAxisTitle = ((from sensor in _sensorsToGraph select sensor.Sensor.Unit).Distinct().Count() == 1) ? _sensorsToGraph[0].Sensor.Unit : String.Empty;
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
            CalculateYAxis();
            if (recalculateDateRange)
                CalculateGraphedEndPoints();
            NotifyOfPropertyChange(() => CanEditDates);
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

        public void AddToGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (_sensorsToGraph.FirstOrDefault(x => x.BoundsSet) != null)
                graphableSensor.SetUpperAndLowerBounds(StartTime, EndTime);
            _sensorsToGraph.Add(graphableSensor);
            Debug.Print("{0} was added to the graph list", graphableSensor.Sensor);
            DisableFeatures();
            UpdateGraph(_sensorsToGraph.Count < 2);
            UpdateDetectionMethodGraphableSensors();
            EnableFeatures();
            AddToEditingSensors(eventArgs);
        }

        public void RemoveFromGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (_sensorsToGraph.Contains(graphableSensor))
                _sensorsToGraph.Remove(graphableSensor);
            Debug.Print("{0} was removed from the graph list", graphableSensor.Sensor);
            DisableFeatures();
            UpdateGraph(false);
            UpdateDetectionMethodGraphableSensors();
            EnableFeatures();
            RemoveFromEditingSensors(eventArgs);
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
            if (e == null)
                return;

            if ((DateTime)e.OldValue == new DateTime() || (DateTime)e.NewValue < EndTime)
                StartTime = (DateTime)e.NewValue;
            else
                StartTime = (DateTime)e.OldValue;

            foreach (var sensor in _sensorsToGraph)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }

            if ((DateTime)e.OldValue != DateTime.MinValue)
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
        }

        /// <summary>
        /// Fired when the end date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e == null)
                return;

            if ((DateTime)e.OldValue == new DateTime() || (DateTime)e.NewValue > StartTime)
                EndTime = (DateTime)e.NewValue;
            else
                EndTime = (DateTime)e.OldValue;

            foreach (var sensor in _sensorsToGraph)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }

            if ((DateTime)e.OldValue != DateTime.MaxValue)
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
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
            if (_sensorsToCheckMethodsAgainst.Contains(gSensor.Sensor))
                _sensorsToCheckMethodsAgainst.Remove(gSensor.Sensor);
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

        public void AddToEditingSensors(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            _sensorsToCheckMethodsAgainst.Add(graphableSensor.Sensor);
            UpdateDetectionMethodGraphableSensors();
            NotifyOfPropertyChange(() => SensorsForEditing);
            CheckTheseMethodsForThisSensor(_detectionMethods.Where(x => x.IsEnabled), graphableSensor.Sensor);
        }

        public void RemoveFromEditingSensors(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (_sensorsToCheckMethodsAgainst.Contains(graphableSensor.Sensor))
                _sensorsToCheckMethodsAgainst.Remove(graphableSensor.Sensor);
            NotifyOfPropertyChange(() => SensorsForEditing);
            UpdateDetectionMethodGraphableSensors();

            foreach (var detectionMethod in _detectionMethods.Where(x => x.IsEnabled))
            {
                Debug.Print("{0} is enabled checking to remove values", detectionMethod.Name);

                var itemsToRemove =
                    detectionMethod.ListBox.Items.Cast<ErroneousValue>().Where(
                        value => !_sensorsToCheckMethodsAgainst.Contains(value.Owner)).ToList();

                foreach (var erroneousValue in itemsToRemove)
                {
                    detectionMethod.ListBox.Items.Remove(erroneousValue);
                }
                detectionMethod.ListBox.Items.Refresh();
            }
        }

        public void DetectionMethodChanged(SelectionChangedEventArgs eventArgs)
        {
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
                        SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
                    }
                }
            }

            if (eventArgs.AddedItems.Count > 0)
            {
                var newTabItem = eventArgs.AddedItems[0] as TabItem;
                if (newTabItem != null)
                {
                    var newTabItemHeader = newTabItem.Header as TextBlock;
                    var newDetectionMethod = _detectionMethods.FirstOrDefault(x => newTabItemHeader != null && x.Abbreviation == newTabItemHeader.Text);
                    if (newDetectionMethod != null)
                    {
                        Debug.Print("Turning on: {0}", newDetectionMethod.Name);
                        newDetectionMethod.IsEnabled = true;
                        newDetectionMethod.SettingsGrid.IsEnabled = true;
                        newDetectionMethod.ListBox.IsEnabled = true;
                        CheckTheseMethods(new Collection<IDetectionMethod> { newDetectionMethod });
                    }
                }
            }
        }

        #endregion
    }
}