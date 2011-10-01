using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class LogWindowViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private string _logText;

        public LogWindowViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            EventLogger.Changed += EventLogged;
        }

        public string LogText
        {
            get { return _logText; }
            set { _logText = value + "\n";NotifyOfPropertyChange(()=>LogText);}
        }

        public void EventLogged(object sender, EventLoggedArgs e)
        {
            LogText += e.EventLog;
        }
    }
}
