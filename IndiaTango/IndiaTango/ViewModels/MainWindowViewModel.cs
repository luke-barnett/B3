using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public MainWindowViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
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
                //TODO: Refresh the everythings
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

        #endregion

        #region Private Methods

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

            var openFileDialog = new OpenFileDialog { Filter = "CSV Files|*.csv" };

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
            CurrentDataset = new Dataset(new Site(0, newSitesName, "", null, null, null, null));
            CurrentDataset.SaveToFile();
            _dataSetFiles = null;
            NotifyOfPropertyChange(() => SiteNames);
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

        #endregion
    }
}
