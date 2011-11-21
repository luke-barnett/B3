using System;
using System.Collections;
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
using System.Windows.Threading;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using CheckBox = System.Windows.Controls.CheckBox;
using GroupBox = System.Windows.Controls.GroupBox;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;

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

            _minMaxRateofChangeDetector = new MinMaxRateOfChangeDetector();
            _minMaxRateofChangeDetector.GraphUpdateNeeded += UpdateGraph;

            _runningMeanStandardDeviationDetector = new RunningMeanStandardDeviationDetector();
            _runningMeanStandardDeviationDetector.GraphUpdateNeeded += UpdateGraph;

            _runningMeanStandardDeviationDetector.RefreshDetectedValues += delegate
            {
                //TODO:
                /*if (!_selectedMethods.Contains(_runningMeanStandardDeviationDetector))
                    return;
                RemoveDetectionMethod(_runningMeanStandardDeviationDetector);
                AddDetectionMethod(_runningMeanStandardDeviationDetector);*/
            };

            _missingValuesDetector = new MissingValuesDetector();

            _detectionMethods = new List<IDetectionMethod> { _missingValuesDetector, _minMaxRateofChangeDetector, _runningMeanStandardDeviationDetector };

            #endregion

            #region Set Up Behaviours

            var behaviourManager = new BehaviourManager { AllowMultipleEnabled = true };

            #region Zoom Behaviour
            var zoomBehaviour = new CustomZoomBehaviour { IsEnabled = !_inSelectionMode };
            zoomBehaviour.ZoomRequested += (o, e) =>
            {
                StartTime = (DateTime)e.FirstPoint.X;
                EndTime = (DateTime)e.SecondPoint.X;
                foreach (var sensor in _sensorsToGraph)
                {
                    sensor.SetUpperAndLowerBounds(StartTime, EndTime);
                }
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
            };
            zoomBehaviour.ZoomResetRequested += o =>
            {
                foreach (var sensor in _sensorsToGraph)
                {
                    sensor.RemoveBounds();
                }
                SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
                CalculateGraphedEndPoints();
            };

            behaviourManager.Behaviours.Add(zoomBehaviour);
            #endregion

            #region Background Behaviour
            _background = new Canvas { Visibility = Visibility.Collapsed };
            var backgroundBehaviour = new GraphBackgroundBehaviour(_background);
            behaviourManager.Behaviours.Add(backgroundBehaviour);
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
        private DateTime _startTime;
        private DateTime _endTime;
        private bool _inSelectionMode;
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
        private readonly MinMaxRateOfChangeDetector _minMaxRateofChangeDetector;
        private readonly RunningMeanStandardDeviationDetector _runningMeanStandardDeviationDetector;
        private readonly MissingValuesDetector _missingValuesDetector;
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
                UpdateGUI();
            }
        }

        private string[] DataSetFiles
        {
            get { return _dataSetFiles ?? (_dataSetFiles = Dataset.GetAllDataSetFileNames()); }
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
                var siteNames = DataSetFiles.Select(x => x.Substring(x.LastIndexOf('\\') + 1, x.Length - x.LastIndexOf('\\') - 4)).ToArray();
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
            get { return (from sensor in Sensors select new GraphableSensor(sensor)).ToList(); }
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
                    Range = new DoubleRange(value, Range.Maximum);
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
                    Range = new DoubleRange(Range.Minimum, value);
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

        #endregion

        #region Private Methods

        private void UpdateGUI()
        {
            NotifyOfPropertyChange(() => Sensors);
            NotifyOfPropertyChange(() => GraphableSensors);
        }

        private void SampleValues(int numberOfPoints, ICollection<GraphableSensor> sensors)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count() / (numberOfPoints / sensors.Count);
                Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
                if (_sampleRate > 1) ShowBackground();
            }

            var min = MinimumY(generatedSeries);
            var max = MaximumY(generatedSeries);

            Range = min < double.MaxValue ? new DoubleRange(min - (Math.Abs(min * .2)), max + (Math.Abs(max * .2))) : new DoubleRange();

            MinMinimum = (int)Minimum;
            MaxMaximum = (int)Maximum;

            MaxMinimum = (int)Maximum;
            MinMaximum = (int)Math.Ceiling(Minimum);

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
            StartTime = minimum;
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
        }

        private void EnableFeatures()
        {
            FeaturesEnabled = true;
        }

        private void BuildDetectionMethodTabItems()
        {
            var tabItems = _detectionMethods.Select(GenerateTabItemFromDetectionMethod).ToList();
            DetectionTabItems = tabItems;
        }

        private TabItem GenerateTabItemFromDetectionMethod(IDetectionMethod method)
        {
            var tabItem = new TabItem { Header = new TextBlock { Text = method.Abbreviation } };

            var tabItemGrid = new Grid();
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            tabItemGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock { Text = method.Name, FontWeight = FontWeights.Bold, FontSize = 20 };

            Grid.SetRow(title, 0);

            var detectionMethodOptions = new GroupBox { Header = new TextBlock { Text = "Options" }, BorderBrush = Brushes.OrangeRed };

            var optionsStackPanel = new StackPanel { Orientation = Orientation.Vertical };
            detectionMethodOptions.Content = optionsStackPanel;

            var enabledCheckBox = new CheckBox { Content = new TextBlock { Text = "Enabled" } };
            enabledCheckBox.Checked += (o, e) =>
                                           {
                                               method.IsEnabled = true;
                                               CheckTheseMethods(new Collection<IDetectionMethod> { method });
                                           };

            enabledCheckBox.Unchecked += (o, e) =>
                                             {
                                                 method.IsEnabled = false;
                                                 //TODO:
                                             };

            optionsStackPanel.Children.Add(enabledCheckBox);

            optionsStackPanel.Children.Add(method.SettingsGrid);

            Grid.SetRow(detectionMethodOptions, 1);

            tabItemGrid.Children.Add(detectionMethodOptions);

            var detectionMethodListBox = new GroupBox { Header = new TextBlock { Text = "Detected Values" }, BorderBrush = Brushes.OrangeRed };

            var listBox = new ListBox();

            method.ListBox = listBox;

            detectionMethodListBox.Content = listBox;

            Grid.SetRow(detectionMethodListBox, 2);

            tabItemGrid.Children.Add(detectionMethodListBox);


            tabItem.Content = tabItemGrid;
            return tabItem;
        }

        private void AddToListBox(ListBox listBox, IEnumerable<ErroneousValue> values)
        {
            foreach (var erroneousValue in values)
            {
                listBox.Items.Add(erroneousValue);
            }
        }

        private void CheckTheseMethods(IEnumerable<IDetectionMethod> methodsToCheck)
        {
            var bw = new BackgroundWorker();

            bw.DoWork += (o, e) =>
                             {
                                 foreach (var detectionMethod in methodsToCheck)
                                 {
                                     if (detectionMethod.ListBox.Dispatcher.CheckAccess())
                                         detectionMethod.ListBox.Items.Clear();
                                     else
                                     {
                                         var method = detectionMethod;
                                         detectionMethod.ListBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                             (System.Action)(() => method.ListBox.Items.Clear()));
                                     }
                                     foreach (var sensor in _sensorsToCheckMethodsAgainst)
                                     {
                                         WaitEventString = string.Format("Checking {0} for {1}", sensor.Name, detectionMethod.Name);
                                         var values =
                                             detectionMethod.GetDetectedValues(sensor).Where(
                                                 x => x.TimeStamp >= StartTime && x.TimeStamp <= EndTime);
                                         if (detectionMethod.ListBox.Dispatcher.CheckAccess())
                                         {
                                             AddToListBox(detectionMethod.ListBox, values);
                                         }
                                         else
                                         {
                                             var method = detectionMethod;
                                             detectionMethod.ListBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                 (System.Action)(() => AddToListBox(method.ListBox, values)));
                                         }
                                     }
                                 }
                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             EnableFeatures();
                                             ShowProgressArea = false;
                                         };

            ShowProgressArea = true;
            ProgressIndeterminate = true;
            DisableFeatures();
            bw.RunWorkerAsync();
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
                                 List<Sensor> sensors = null;
                                 try
                                 {
                                     sensors = reader.ReadSensors(null, CurrentDataset);
                                 }
                                 catch (Exception ex)
                                 {
                                     Common.ShowMessageBoxWithException("Failed Import", "Bad File Format", false, true, ex);
                                 }

                                 e.Result = sensors;
                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             var sensors = (List<Sensor>)e.Result;

                                             if (sensors == null) return;

                                             if (CurrentDataset.Sensors.Count == 0)
                                                 CurrentDataset.Sensors = sensors;
                                             else
                                             {
                                                 var askUser = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;

                                                 if (askUser == null)
                                                 {
                                                     Common.ShowMessageBox("EPIC FAIL", "RUN AROUND WITH NO REASON", false, true);
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
                                                     var askUserDialog = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;

                                                     if (askUserDialog == null)
                                                         return;

                                                     askUserDialog.Title = "What sensor does this belong to?";
                                                     askUserDialog.ComboBoxItems = new List<string>((from x in CurrentDataset.Sensors select x.Name));
                                                     askUserDialog.ShowComboBox = true;
                                                     askUserDialog.ShowCancel = true;
                                                     askUserDialog.ComboBoxSelectedIndex = 0;
                                                     askUserDialog.CanEditComboBox = false;
                                                     askUserDialog.Message = string.Format("Match {0} against an existing sensor. \n\r Cancel to create a new sensor", sensor.Name);

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
                                                                         askUserDialog.ComboBoxSelectedIndex]) == 0).
                                                                 DefaultIfEmpty(null).FirstOrDefault();

                                                         if (matchingSensor == null)
                                                         {
                                                             Debug.WriteLine("Failed to find the sensor again, embarrasing!");
                                                             continue;
                                                         }

                                                         Debug.WriteLine("Merging sensors");
                                                         //Otherwise clone the current state
                                                         var newState = matchingSensor.CurrentState.Clone();
                                                         //Check to see if values are inserted
                                                         var insertedValues = false;

                                                         //And add values for any new dates we want
                                                         foreach (var value in sensor.CurrentState.Values.Where(value => !keepOldValues || !newState.Values.ContainsKey(value.Key)))
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
                                                             EventLogger.LogSensorInfo(CurrentDataset, matchingSensor.Name, "Added values from new import");
                                                         }
                                                     }
                                                 }
                                             }

                                             UpdateGUI();


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
            var newSitesName = "New Site";
            if (File.Exists(Path.Combine(Common.DatasetSaveLocation, "New Site.b3")))
            {
                var x = 1;
                while (File.Exists(Path.Combine(Common.DatasetSaveLocation, string.Format("New Site{0}.b3", x))))
                    x++;

                newSitesName = "New Site" + x;
            }
            ShowSiteInformation(new Dataset(new Site(0, newSitesName, "", null, null, null, null)));
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
            if (_chosenSelectedIndex < 0)
                return;

            var saveFirst = false;

            if (CurrentDataset != null)
            {
                //Do we want to save the dataset?
                var userPrompt = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;

                if (userPrompt == null)
                {
                    EventLogger.LogError(CurrentDataset, "Changing Sites", "PROMPT DIDN'T LOAD MEGA FAILURE");
                    return;
                }

                userPrompt.Title = "Shall I Save?";
                userPrompt.Message =
                    string.Format(
                        "Do you want to save \"{0}\" before changing dataset \n\r (unsaved progress WILL be lost) ",
                        CurrentDataset.Site.Name);
                userPrompt.ShowCancel = true;
                userPrompt.ShowComboBox = true;
                userPrompt.ComboBoxItems = new List<string> { "Yes", "No" };
                userPrompt.CanEditComboBox = false;
                userPrompt.ComboBoxSelectedIndex = 0;

                _windowManager.ShowDialog(userPrompt);

                if (userPrompt.WasCanceled)
                    return;

                if (userPrompt.ComboBoxSelectedIndex == 0)
                    saveFirst = true;
            }

            Debug.Print("Chosen Selected Index {0}", _chosenSelectedIndex);

            foreach (var file in DataSetFiles)
            {
                Debug.WriteLine(file);
            }

            Debug.Print("Chosen file is {0}", DataSetFiles[_chosenSelectedIndex]);

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
                WaitEventString = string.Format("Loading from {0}", DataSetFiles[_chosenSelectedIndex]);
                CurrentDataset = Dataset.LoadDataSet(DataSetFiles[_chosenSelectedIndex]);
                EventLogger.LogInfo(null, "Loaded dataset", string.Format("Loaded {0}", DataSetFiles[_chosenSelectedIndex]));
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
        public void UpdateGraph()
        {
            Debug.WriteLine("Updating Graph");
            ChartTitle = (_sensorsToGraph.Count > 0) ? string.Format("{0} [{1}m]", _sensorsToGraph[0].Sensor.Name, _sensorsToGraph[0].Sensor.Depth) : String.Empty;

            for (var i = 1; i < _sensorsToGraph.Count; i++)
                ChartTitle += string.Format(" and {0} [{1}m]", _sensorsToGraph[i].Sensor.Name, _sensorsToGraph[i].Sensor.Depth);

            YAxisTitle = ((from sensor in _sensorsToGraph select sensor.Sensor.Unit).Distinct().Count() == 1) ? _sensorsToGraph[0].Sensor.Unit : String.Empty;

            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
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
                Save();

            Debug.WriteLine("Closing Program");
        }

        public void AddToGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            _sensorsToGraph.Add(graphableSensor);
            Debug.Print("{0} was added to the graph list", graphableSensor.Sensor);
            UpdateGraph();
        }

        public void RemoveFromGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (_sensorsToGraph.Contains(graphableSensor))
                _sensorsToGraph.Remove(graphableSensor);
            Debug.Print("{0} was removed from the graph list", graphableSensor.Sensor);
            UpdateGraph();
        }

        public void ShowCurrentSiteInformation()
        {
            ShowSiteInformation(CurrentDataset);
        }

        /// <summary>
        /// Fired when the start date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void StartTimeChanged(RoutedPropertyChangedEventArgs<DateTime> e)
        {
            if (e == null)
                return;

            if (e.OldValue == new DateTime() || e.NewValue < EndTime)
                StartTime = e.NewValue;
            else
                StartTime = e.OldValue;

            foreach (var sensor in _sensorsToGraph)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
        }

        /// <summary>
        /// Fired when the end date is changed
        /// </summary>
        /// <param name="e">The event arguments about the new date</param>
        public void EndTimeChanged(RoutedPropertyChangedEventArgs<DateTime> e)
        {
            if (e == null)
                return;

            if (e.OldValue == new DateTime() || e.NewValue > StartTime)
                EndTime = e.NewValue;
            else
                EndTime = e.OldValue;

            foreach (var sensor in _sensorsToGraph)
            {
                sensor.SetUpperAndLowerBounds(StartTime, EndTime);
            }
            SampleValues(Common.MaximumGraphablePoints, _sensorsToGraph);
        }

        public void ColourChanged(RoutedPropertyChangedEventArgs<Color> args)
        {
            Debug.Print("Colour changed from {0} to {1}", args.OldValue, args.NewValue);

            if (_sensorsToGraph.Count > 0)
            {
                UpdateGraph();
            }
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
            Debug.Print("{0} was added to the editing list", graphableSensor.Sensor);
            UpdateGraph();
        }

        public void RemoveFromEditingSensors(RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (_sensorsToCheckMethodsAgainst.Contains(graphableSensor.Sensor))
                _sensorsToCheckMethodsAgainst.Remove(graphableSensor.Sensor);
            Debug.Print("{0} was removed from the editing list", graphableSensor.Sensor);
            UpdateGraph();
        }

        #endregion
    }
}
