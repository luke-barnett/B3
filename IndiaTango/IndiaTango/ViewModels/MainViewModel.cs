using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using MessageBox = System.Windows.Forms.MessageBox;

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

		public string TagLine { get { return ApplicationTagLine; } }

        public void BtnNew()
        {
            EventLogger.LogInfo(GetType().ToString(), "Starting a new session...");
            System.Diagnostics.Debug.Print("Window manager null {0}", _windowManager == null);
            System.Diagnostics.Debug.Print("container null {0}", _container == null);
            _windowManager.ShowDialog(_container.GetInstance(typeof(SessionViewModel), "SessionViewModel"));
        }
        
        public void BtnLoad()
        {
            EventLogger.LogInfo(GetType().ToString(), "Loading a new session...");
        	Common.ShowFeatureNotImplementedMessageBox();
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
