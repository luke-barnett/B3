using System;
using System.Linq;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class LogWindowViewModel : BaseViewModel
    {
        private int _selectedModeIndex;
        private string[] _logs;
        private string[] _logFiles;
        private int _selectedLogFileIndex;

        public LogWindowViewModel()
        {
            EventLogger.Changed += EventLogged;
            LogFiles = EventLogger.GetLogFiles();
            SelectedLogFileIndex = 0;
            SelectedModeIndex = 0;
        }

        public void EventLogged(object sender, EventLoggedArgs e)
        {
            if (!_logFiles.Contains(e.Filename))
            {
                var currentlySelected = _logFiles[_selectedLogFileIndex];
                LogFiles = EventLogger.GetLogFiles();
                SelectedLogFileIndex = Array.IndexOf(LogFiles, currentlySelected);
            }

            if (e.Filename != LogFiles[SelectedLogFileIndex]) return;

            var temp = new string[Logs.Length + 1];
            for (var i = 0; i < Logs.Length; i++)
            {
                temp[i] = Logs[i];
            }
            temp[Logs.Length] = e.EventLog;
            Logs = temp;
        }

        public string[] Modes
        {
            get
            {
                return new[] { "ALL", "INFO", "WARNING", "ERROR" };
            }
        }

        public int SelectedModeIndex
        {
            get { return _selectedModeIndex; }
            set
            {
                _selectedModeIndex = value;
                NotifyOfPropertyChange(() => Logs);
            }
        }

        public string Title
        {
            get { return "Log"; }
        }

        public string[] LogFiles
        {
            get { return _logFiles; }
            set
            {
                _logFiles = value;
                NotifyOfPropertyChange(() => LogFiles);
            }
        }

        public int SelectedLogFileIndex
        {
            get { return _selectedLogFileIndex; }
            set
            {
                _selectedLogFileIndex = value;
                NotifyOfPropertyChange(() => SelectedLogFileIndex);
                Logs = EventLogger.GetLast20FromFile(LogFiles[SelectedLogFileIndex]);
            }
        }

        public string[] Logs
        {
            get { return _logs.Where(x => SelectedModeIndex == 0 || x.Contains(Modes[SelectedModeIndex])).ToArray(); }
            set
            {
                _logs = value;
                NotifyOfPropertyChange(() => Logs);
            }
        }
    }
}
