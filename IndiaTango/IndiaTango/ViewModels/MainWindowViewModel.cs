using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using DataGrid = System.Windows.Controls.DataGrid;

namespace IndiaTango.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public MainWindowViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            _selectedSensors = new List<GraphableSensor>();

            #region Set Up Behaviours

            var behaviourManager = new BehaviourManager { AllowMultipleEnabled = true };

            #region Zoom Behaviour
            var zoomBehaviour = new CustomZoomBehaviour { IsEnabled = !_inSelectionMode };
            zoomBehaviour.ZoomRequested += (o, e) =>
            {
                StartTime = (DateTime)e.FirstPoint.X;
                EndTime = (DateTime)e.SecondPoint.X;
                foreach (var sensor in _selectedSensors)
                {
                    sensor.SetUpperAndLowerBounds(StartTime, EndTime);
                }
                SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
            };
            zoomBehaviour.ZoomResetRequested += o =>
            {
                foreach (var sensor in _selectedSensors)
                {
                    sensor.RemoveBounds();
                }
                SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
                CalculateGraphedEndPoints();
            };

            behaviourManager.Behaviours.Add(zoomBehaviour);
            #endregion

            Behaviour = behaviourManager;

            #endregion
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
        private readonly List<GraphableSensor> _selectedSensors;
        private int _sampleRate;
        private DateTime _startTime;
        private DateTime _endTime;
        private bool _inSelectionMode;
        #endregion
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
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }
        /// <summary>
        /// The start of the time period being displayed
        /// </summary>
        public DateTime StartTime { get { return _startTime; } set { _startTime = value; NotifyOfPropertyChange(() => StartTime); } }
        /// <summary>
        /// The end of the time period being displayed
        /// </summary>
        public DateTime EndTime { get { return _endTime; } set { _endTime = value; NotifyOfPropertyChange(() => EndTime); } }
        #endregion

        #endregion

        #region Private Methods

        private void UpdateGUI()
        {
            NotifyOfPropertyChange(() => Sensors);
            NotifyOfPropertyChange(() => GraphableSensors);
        }

        private void UpdateGraph()
        {
            ChartTitle = (_selectedSensors.Count > 0) ? string.Format("{0} [{1}m]", _selectedSensors[0].Sensor.Name, _selectedSensors[0].Sensor.Depth) : String.Empty;

            for (var i = 1; i < _selectedSensors.Count; i++)
                ChartTitle += string.Format(" and {0} [{1}m]", _selectedSensors[i].Sensor.Name, _selectedSensors[i].Sensor.Depth);

            YAxisTitle = ((from sensor in _selectedSensors select sensor.Sensor.Unit).Distinct().Count() == 1) ? _selectedSensors[0].Sensor.Unit : String.Empty;

            SampleValues(Common.MaximumGraphablePoints, _selectedSensors);
            CalculateGraphedEndPoints();
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

            ChartSeries = generatedSeries;
        }

        private void HideBackground()
        {
            //TODO: Write This
        }

        private void ShowBackground()
        {
            //TODO: Write this
        }

        private void CalculateGraphedEndPoints()
        {
            var minimum = DateTime.MaxValue;
            var maximum = DateTime.MinValue;

            foreach (var sensor in _selectedSensors)
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
                                     sensors = reader.ReadSensors();
                                 }
                                 catch (Exception ex)
                                 {
                                     Common.ShowMessageBoxWithException("Failed Import", "Bad File Format", false, true, ex);
                                 }

                                 if (sensors != null)
                                 {
                                     //TODO: MATCH ALREADY EXISTING SENSORS
                                     CurrentDataset.Sensors = sensors;

                                     UpdateGUI();
                                 }
                             };

            bw.RunWorkerCompleted += (o, e) =>
                                         {
                                             ShowProgressArea = false;
                                             //TODO: Re-enable the things that we disabled
                                         };
            //TODO: Disable the things
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
                                             //TODO: Renable locked out features
                                         };
            ProgressIndeterminate = true;
            ShowProgressArea = true;
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
                //TODO: Reset all waiting
            };
            //TODO: Freeze all users work while we load

            bw.RunWorkerAsync();
        }

        #endregion

        #region Event Handlers

        public void ClosingRequested(CancelEventArgs eventArgs)
        {
            //TODO: Check for save

            Debug.WriteLine("Closing Program");
        }

        public void AddToGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (System.Windows.Controls.CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            _selectedSensors.Add(graphableSensor);
            Debug.Print("{0} was added to the graph list", graphableSensor.Sensor);
            UpdateGraph();
        }

        public void RemoveFromGraph(RoutedEventArgs eventArgs)
        {
            var checkBox = (System.Windows.Controls.CheckBox)eventArgs.Source;
            var graphableSensor = (GraphableSensor)checkBox.Content;
            if (_selectedSensors.Contains(graphableSensor))
                _selectedSensors.Remove(graphableSensor);
            Debug.Print("{0} was removed from the graph list", graphableSensor.Sensor);
            UpdateGraph();
        }

        public void CheckEdit(DataGridCellEditEndingEventArgs eventArgs, DataGrid dataGrid)
        {
            Debug.Print("Current selected value is {0}", dataGrid.SelectedValue);
            Debug.Print("The editing element is a {0}", eventArgs.EditingElement);
            if ((string)eventArgs.Column.Header == "Depth")
            {
                try
                {

                }
                catch (Exception)
                {
                    eventArgs.Cancel = true;
                }
            }
        }

        public void ShowCurrentSiteInformation()
        {
            ShowSiteInformation(CurrentDataset);
        }

        #endregion
    }
}
