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

        /// <summary>
        /// Updates the logs to reflect a new event being logged
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event log arguments</param>
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

        /// <summary>
        /// The list of modes to show logs for
        /// </summary>
        public string[] Modes
        {
            get
            {
                return new[] { "ALL", "INFO", "WARNING", "ERROR" };
            }
        }

        /// <summary>
        /// The index of the currently selected mode
        /// </summary>
        public int SelectedModeIndex
        {
            get { return _selectedModeIndex; }
            set
            {
                _selectedModeIndex = value;
                NotifyOfPropertyChange(() => Logs);
            }
        }

        /// <summary>
        /// The window title
        /// </summary>
        public string Title
        {
            get { return "Log"; }
        }

        /// <summary>
        /// The list of log files
        /// </summary>
        public string[] LogFiles
        {
            get { return _logFiles; }
            set
            {
                _logFiles = value;
                NotifyOfPropertyChange(() => LogFiles);
            }
        }

        /// <summary>
        /// The index of the currently selected log file
        /// </summary>
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

        /// <summary>
        /// The current list of logs being displayed
        /// </summary>
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
