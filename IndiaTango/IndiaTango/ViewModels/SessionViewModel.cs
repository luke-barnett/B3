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

        public string LoadingProgress { get { return string.Format("{0}%", ProgressBarPercent); } }

        private bool _buttonsEnabled = true;
        public bool ButtonsEnabled { get { return _buttonsEnabled; } set { _buttonsEnabled = value; NotifyOfPropertyChange(() => ButtonsEnabled); NotifyOfPropertyChange(() => LoadingComponentsVisible); } }

        public string LoadingComponentsVisible { get { return (_buttonsEnabled) ? "Hidden" : "Visible"; } }

        public List<Sensor> SensorList { get { return _sensors; } set { _sensors = value; NotifyOfPropertyChange(() => SensorList); } }

        private Sensor _itemList1Selected = null;
        public Sensor ItemList1Selected { get { return _itemList1Selected; } set { _itemList1Selected = value; NotifyOfPropertyChange(() => _itemList1Selected); } }

        public void btnGraph()
        {
            if (ItemList1Selected == null)
                return;
            var graphView = (_container.GetInstance(typeof (GraphViewModel), "GraphViewModel") as GraphViewModel);
            graphView.ChartTitle = ItemList1Selected.Name;

            var chartSeries = new DataSeries<DateTime, float>();
            foreach(var dataValue in _itemList1Selected.CurrentState.Values)
            {
                chartSeries.Add(dataValue.Timestamp,dataValue.Value);
            }
            chartSeries.Title = ItemList1Selected.Name;
            graphView.YAxisTitle = chartSeries.Title;
            graphView.ChartSeries = chartSeries;
            _windowManager.ShowWindow(graphView);
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
