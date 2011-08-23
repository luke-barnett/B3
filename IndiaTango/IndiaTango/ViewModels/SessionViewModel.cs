using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    class SessionViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private List<Sensor> _sensors;
        private BackgroundWorker _bw;

        public SessionViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _sensors = new List<Sensor>();
        }

        public void btnImport()
        {
            _bw = new BackgroundWorker();
            _bw.WorkerReportsProgress = true;
            _bw.WorkerSupportsCancellation = true;

            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "CSV Files|*.csv";
            var result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                _bw.DoWork += delegate(object sender, DoWorkEventArgs eventArgs)
                                 {
                                     ButtonsEnabled = false;
                                     var reader = new CSVReader(fileDialog.FileName);
                                     reader.ProgressChanged += delegate(object o, ReaderProgressChangedArgs e)
                                                                   {
                                                                       ProgressBarPercent = e.Progress;
                                                                   };
                                     SensorList = reader.ReadSensors(_bw);

                                     if (SensorList == null)
                                     {
                                         eventArgs.Cancel = true;
                                         ProgressBarPercent = 0;
                                     }

                                     ButtonsEnabled = true;
                                 };
                _bw.RunWorkerAsync();
            }
        }

        private static double _progressBarPercent = 0;
        public double ProgressBarPercent
        { 
            get { return _progressBarPercent; } 
            set
            { 
                _progressBarPercent = value;

                NotifyOfPropertyChange(() => ProgressBarPercent);
                NotifyOfPropertyChange(() => LoadingProgress);
                NotifyOfPropertyChange(() => ProgressBarPercentDouble);
            }
        }

        public double ProgressBarPercentDouble
        {
            get { return ProgressBarPercent/100; }
        }

        public string Title
        {
            get { return "New Session"; }
        }

        public string LoadingProgress { get { return string.Format("{0}%", ProgressBarPercent); } }

        private bool _buttonsEnabled = true;
        public bool ButtonsEnabled { get { return _buttonsEnabled; } set { _buttonsEnabled = value; NotifyOfPropertyChange(() => ButtonsEnabled); NotifyOfPropertyChange(() => LoadingComponentsVisible); NotifyOfPropertyChange(()=>ProgressState);} }

        public string LoadingComponentsVisible { get { return (_buttonsEnabled) ? "Hidden" : "Visible"; } }
        public string ProgressState { get { return _buttonsEnabled ? "None" : "Normal"; } }

        public List<Sensor> SensorList { get { return _sensors; } set { _sensors = value; NotifyOfPropertyChange(() => SensorList); } }

        private Sensor _selectedSensor = null;
        public Sensor SelectedSensor { get { return _selectedSensor; } set { _selectedSensor = value; NotifyOfPropertyChange(() => _selectedSensor); } }

        public void btnGraph()
        {
            if (SelectedSensor == null)
            {
                MessageBox.Show("You must first import a data set and select a parameter to be graphed.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var graphView = (_container.GetInstance(typeof (GraphViewModel), "GraphViewModel") as GraphViewModel);
            graphView.Sensor = SelectedSensor;
            _windowManager.ShowWindow(graphView);
        }

        public void btnDetails()
        {
            var detailView =
                (_container.GetInstance(typeof (BuoyDetailsViewModel), "BuoyDetailsViewModel") as BuoyDetailsViewModel);
            _windowManager.ShowDialog(detailView);
        }

        public void btnCancel()
        {
            if(_bw != null)
            {
                try
                {
                    _bw.CancelAsync();
                    ButtonsEnabled = true;
                }
                catch (InvalidOperationException ex)
                {
                    // TODO: meaningful error here
                    System.Diagnostics.Debug.WriteLine("Cannot cancel data loading thread - " + ex);
                }
            }
        }

    }
}
