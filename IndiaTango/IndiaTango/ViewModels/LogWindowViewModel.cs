using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class LogWindowViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private string _logText;
        private ComboBoxItem _mode;

        public LogWindowViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            EventLogger.Changed += EventLogged;
            _logText = "";
        }

        public string LogText
        {
            get
            {
                var returnString = "";
                if(_mode!=null && (string)_mode.Content!="All" && _logText != "")
                {
                    var lines = _logText.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line == "") continue;
                        var parts = line.Split(' ');
                        if (parts[5] == (string)_mode.Content)
                            returnString += line + "\n";
                    }
                    return returnString;
                }
                return _logText;
            }
            set { _logText = value + "\n";NotifyOfPropertyChange(()=>LogText);}
        }

        public void EventLogged(object sender, EventLoggedArgs e)
        {
            LogText += e.EventLog;
        }

        public ComboBoxItem Mode
        {
            get { return _mode; }
            set { _mode = value;
                NotifyOfPropertyChange(() => Mode); NotifyOfPropertyChange(() => LogText); }
        }

        public string Title
        {
            get { return "Log"; }
        }
    }
}
