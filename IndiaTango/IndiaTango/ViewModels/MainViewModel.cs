using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;

namespace IndiaTango.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
		private Cursor _viewCursor = Cursors.Arrow;

        public MainViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public string Title { get { return ApplicationTitle; } }

		public string TagLine { get { return ApplicationTagLine; } }

		public Cursor ViewCursor
		{
			get { return _viewCursor; }
			set { _viewCursor = value; NotifyOfPropertyChange(() => ViewCursor); }
		}

        public void BtnNew()
        {
            EventLogger.LogInfo(GetType().ToString(), "Starting a new session...");
            System.Diagnostics.Debug.Print("Window manager null {0}", _windowManager == null);
            System.Diagnostics.Debug.Print("container null {0}", _container == null);
            _windowManager.ShowDialog(_container.GetInstance(typeof(SessionViewModel), "SessionViewModel"));
        }
        
        public void BtnLoad()
        {
            EventLogger.LogInfo(GetType().ToString(), "Loading a session...");
            var openFileDialog = new OpenFileDialog { Filter = "Session Files|*.indiatango" };

			ViewCursor = Cursors.Wait;
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
            	
                var sessionView = (SessionViewModel)_container.GetInstance(typeof(SessionViewModel), "SessionViewModel");
                using(var stream = new FileStream(openFileDialog.FileName, FileMode.Open))
                    sessionView.Dataset = (Dataset)new BinaryFormatter().Deserialize(stream);
            	
                _windowManager.ShowDialog(sessionView);
            }
			ViewCursor = Cursors.Arrow;
        }

        public void BtnSettings()
        {
            var settingsView =
                (SettingsViewModel) _container.GetInstance(typeof (SettingsViewModel), "SettingsViewModel");
            _windowManager.ShowDialog(settingsView);
        }

        public void OnLoaded()
        {
            EventLogger.LogInfo(GetType().ToString(), "Program started.");
        }

        public void OnUnloaded()
        {
            EventLogger.LogInfo(GetType().ToString(), "Program exited.");
        }
    }
}
