﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private IWindowManager _windowManager;
        /// <summary>
        /// The container holding all the views
        /// </summary>
        private SimpleContainer _container;

        /// <summary>
        /// The current data set being used
        /// </summary>
        private Dataset _currentDataset;

        private string[] _dataSetFiles;

        private int _chosenSelectedIndex;

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
            }
        }

        private string[] DataSetFiles
        {
            get { return _dataSetFiles ?? (_dataSetFiles = Dataset.GetAllDataSetFileNames()); }
        }

        #endregion

        #region Public Properties

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

        public int ChosenSelectedIndex
        {
            get { return (CurrentDataset != null) ?  _chosenSelectedIndex : -1; }
            set
            {
                _chosenSelectedIndex = value;
                NotifyOfPropertyChange(() => ChosenSelectedIndex);

                //Load the Dataset
                if (CurrentDataset != null && (DataSetFiles[ChosenSelectedIndex] == CurrentDataset.Site.Name))
                    return;

                var bw = new BackgroundWorker();

                bw.DoWork += (o, e) =>
                                 {
                                     CurrentDataset = Dataset.LoadDataSet(DataSetFiles[_chosenSelectedIndex]);
                                 };
                bw.RunWorkerCompleted += (o, e) =>
                                             {
                                                 //TODO: Reset all waiting
                                             };
                //TODO: Freeze all users work while we load

                bw.RunWorkerAsync();
            }
        }

        #endregion

        #region Private Methods

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts a new import of data
        /// </summary>
        public void Import()
        {

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
