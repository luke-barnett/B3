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

        public SessionViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _sensors = new List<Sensor>();
        }

        public void btnImport()
        {
            var bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            var fileDialog = new OpenFileDialog();
            fileDialog.ShowDialog();
            bw.DoWork += delegate(object sender, DoWorkEventArgs eventArgs)
                             {
                                 ButtonsEnabled = false;
                                 SensorList = new CSVReader(fileDialog.FileName).ReadSensors();
                                 ButtonsEnabled = true;
                             };
            bw.RunWorkerAsync();
        }

        private bool _buttonsEnabled = true;
        public bool ButtonsEnabled { get { return _buttonsEnabled; } set { _buttonsEnabled = value; NotifyOfPropertyChange(() => ButtonsEnabled); NotifyOfPropertyChange(() => ProgressBarVisible); } }

        public string ProgressBarVisible { get { return (_buttonsEnabled) ? "Hidden" : "Visible"; } }

        public List<Sensor> SensorList { get { return _sensors; } set { _sensors = value; NotifyOfPropertyChange(() => SensorList); } }

        private Sensor _itemList1Selected = null;
        public Sensor ItemList1Selected { get { return _itemList1Selected; } set { _itemList1Selected = value; NotifyOfPropertyChange(() => _itemList1Selected); } }

        public void btnGraph()
        {
            if (ItemList1Selected == null)
            {
                MessageBox.Show("YOU FOOL");
                return;
            }
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

    }
}
