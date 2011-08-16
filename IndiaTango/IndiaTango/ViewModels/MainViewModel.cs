using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Caliburn.Micro;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;

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
                bw.DoWork();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void BtnGraphView()
        {
            _windowManager.ShowDialog(_container.GetInstance(typeof(GraphViewModel), "GraphViewModel"));
        }
    }
}
