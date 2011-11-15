using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using IndiaTango.Views;
using Cursors = System.Windows.Input.Cursors;

namespace IndiaTango.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private bool _buttonsEnabled = true;
        private Dataset dataset { get; set; }
        private LogWindowViewModel _logWindow;

        public MainViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _logWindow =
                (LogWindowViewModel)_container.GetInstance(typeof(LogWindowViewModel), "LogWindowViewModel");
            //_logWindow = LogWindowViewModel();
        }

        public string Title { get { return ApplicationTitle; } }

		public string TagLine { get { return ApplicationTagLine; } }

        public bool ButtonsEnabled { get { return _buttonsEnabled; } set { _buttonsEnabled = value; NotifyOfPropertyChange(() => ButtonsEnabled); } }

        public void BtnNew()
        {
            EventLogger.LogInfo(null, GetType().ToString(), "Starting a new session...");
            System.Diagnostics.Debug.Print("Window manager null {0}", _windowManager == null);
            System.Diagnostics.Debug.Print("container null {0}", _container == null);
            _windowManager.ShowWindow(_container.GetInstance(typeof(SessionViewModel), "SessionViewModel"));
        }
        
        public void BtnLoad()
        {
            EventLogger.LogInfo(null, GetType().ToString(), "Loading a session...");
            var openFileDialog = new OpenFileDialog { Filter = "Site Files|*.b3;*.indiatango" };
            
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ApplicationCursor = Cursors.Wait;
                ButtonsEnabled = false;
                var bw = new BackgroundWorker();
                bw.DoWork += (o, e) =>
                                 {
                                     EventLogger.LogInfo(null, "Loading", "Started loading from file");
                                     using (var stream = new FileStream(openFileDialog.FileName, FileMode.Open))
                                         e.Result = new BinaryFormatter().Deserialize(stream);
                                     EventLogger.LogInfo(null, "Loading", "Loading from file completed");
                                 };
                bw.RunWorkerCompleted += (o, e) =>
                                             {
                                                 if (e.Cancelled || e.Error != null)
                                                     return;
                                                 var sessionView = (SessionViewModel)_container.GetInstance(typeof(SessionViewModel), "SessionViewModel");
                                                 sessionView.Dataset = (Dataset) e.Result;

                                                 ApplicationCursor = Cursors.Arrow;
                                                 ButtonsEnabled = true;
                                                 EventLogger.LogInfo(null, GetType().ToString(), "Loading Session View");
                                                 _windowManager.ShowWindow(sessionView);
                                             };
                bw.RunWorkerAsync();
            }
        }

        public void BtnSettings()
        {
            var settingsView =
                (SettingsViewModel) _container.GetInstance(typeof (SettingsViewModel), "SettingsViewModel");
            _windowManager.ShowDialog(settingsView);
        }

        public void OnLoaded()
        {
            EventLogger.LogInfo(null, GetType().ToString(), "Program started.");
        }

        public void OnUnloaded()
        {
            EventLogger.LogInfo(null, GetType().ToString(), "Program exited.");
        }

        public void BtnLog()
        {
            if (!_logWindow.IsActive)
                _windowManager.ShowWindow(_logWindow);
            else
                ((LogWindowView) _logWindow.GetView()).Focus();
        }   
    }
}
