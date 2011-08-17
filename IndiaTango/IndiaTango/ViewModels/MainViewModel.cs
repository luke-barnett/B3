using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Caliburn.Micro;
using Visiblox.Charts;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private static LoadViewModel _lvm;
        public MainViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public string Title { get { return ApplicationTitle; } }

        public void BtnNew()
        {
            System.Diagnostics.Debug.Print("Window manager null {0}", _windowManager == null);
            System.Diagnostics.Debug.Print("container null {0}", _container == null);
            _windowManager.ShowDialog(_container.GetInstance(typeof(MainViewModel), "MainViewModel"));
        }
        
        public void BtnLoad()
        {
            try
            {
                var openCsv = new OpenFileDialog();
                openCsv.Filter = "CSV Files|*.csv|All Files|*.*";
                if (!((bool) openCsv.ShowDialog())) return;
                System.Diagnostics.Debug.Print("File loaded: {0}",openCsv.FileName);
                var reader = new CSVReader(openCsv.FileName);
                var bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.DoWork += delegate(object sender, DoWorkEventArgs e)
                                 {
                                     var sensors = new List<Sensor>();
                                     bw.ReportProgress(50);
                                     sensors = reader.ReadSensors();
                                 };
                bw.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e)
                                          {
                                              _lvm.ProgressValue = e.ProgressPercentage;
                                          };
                bw.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
                                             {
                                                 
                                             };
                _lvm = (_container.GetInstance(typeof (LoadViewModel), "LoadViewModel") as LoadViewModel);
                _windowManager.ShowWindow(_lvm);
                bw.RunWorkerAsync();

            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        static void BwDoWork(object sender,DoWorkEventArgs e)
        {
            var args = (object[])e.Argument;
            var reader = (CSVReader)args[0];
            var sensors = (List<Sensor>)args[1];
            (sender as BackgroundWorker).ReportProgress(50);
            sensors = reader.ReadSensors();
        }

        static void BwProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _lvm.ProgressValue = e.ProgressPercentage;
        }

        public void BtnGraphView()
        {
            var graphViewModel = _container.GetInstance(typeof (GraphViewModel), "GraphViewModel") as GraphViewModel;
            if (graphViewModel != null)
            {
                graphViewModel.ChartTitle = "New Graph";
                var chartSeries = new DataSeries<DateTime, float>();
                var generator = new Random();
                for (int i = 0; i < 500; i++ )
                {
                    chartSeries.Add((DateTime.Now).AddHours(i), (float)generator.NextDouble()*i);
                }
                chartSeries.Title = "Random Data";
                graphViewModel.ChartSeries = chartSeries;
                graphViewModel.YAxisTitle = "Points of Awesome";
                _windowManager.ShowDialog(graphViewModel);
            }
            else
                MessageBox.Show("Failed to get a GraphViewModel");
        }
    }
}
