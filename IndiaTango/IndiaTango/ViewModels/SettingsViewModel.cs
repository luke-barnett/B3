using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class SettingsViewModel : BaseViewModel
    {

        private bool _dontNotifyIfFailing;
        private bool _formulaValidationAsTyped;
        private int _errorThreshold;
        private int _autoSaveInterval;
        private bool _autoSaveEnabled;

        public SettingsViewModel()
        {
            // Load in settings
            DontNotifyIfFailing = Properties.Settings.Default.IgnoreSensorErrorDetection;
            FormulaValidationAsTyped = Properties.Settings.Default.EvaluateFormulaOnKeyUp;
            ErrorThreshold = Properties.Settings.Default.DefaultErrorThreshold;
            AutoSaveInterval = Properties.Settings.Default.AutoSaveTimerInterval / 60000;
            AutoSaveEnabled = Properties.Settings.Default.AutoSaveTimerEnabled;
        }

        /// <summary>
        /// The title of the window
        /// </summary>
        public string WindowTitle
        {
            get { return "Settings"; }
        }

        /// <summary>
        /// The heading title
        /// </summary>
        public string Title
        {
            get { return "Configure " + Common.ApplicationTitle; }
        }

        /// <summary>
        /// Whether or not to notify if failing
        /// </summary>
        public bool DontNotifyIfFailing
        {
            get { return _dontNotifyIfFailing; }
            set { _dontNotifyIfFailing = value; NotifyOfPropertyChange(() => DontNotifyIfFailing); }
        }

        /// <summary>
        /// Whetheror not to validate formulas as typed
        /// </summary>
        public bool FormulaValidationAsTyped
        {
            get { return _formulaValidationAsTyped; }
            set { _formulaValidationAsTyped = value; NotifyOfPropertyChange(() => FormulaValidationAsTyped); }
        }

        /// <summary>
        /// The error threshold for sensor warnings
        /// </summary>
        public int ErrorThreshold
        {
            get { return _errorThreshold; }
            set { _errorThreshold = value; NotifyOfPropertyChange(() => ErrorThreshold); }
        }

        /// <summary>
        /// The interval for auto save
        /// </summary>
        public int AutoSaveInterval
        {
            get { return _autoSaveInterval; }
            set { _autoSaveInterval = value; NotifyOfPropertyChange(() => AutoSaveInterval); }
        }

        /// <summary>
        /// If auto save is enabled or not
        /// </summary>
        public bool AutoSaveEnabled
        {
            get { return _autoSaveEnabled; }
            set { _autoSaveEnabled = value; NotifyOfPropertyChange(() => AutoSaveEnabled); }
        }

        /// <summary>
        /// Saves the settings and closes the window
        /// </summary>
        public void BtnSave()
        {
            Properties.Settings.Default.DefaultErrorThreshold = ErrorThreshold;
            Properties.Settings.Default.EvaluateFormulaOnKeyUp = FormulaValidationAsTyped;
            Properties.Settings.Default.IgnoreSensorErrorDetection = DontNotifyIfFailing;
            Properties.Settings.Default.AutoSaveTimerInterval = AutoSaveInterval * 60000;
            Properties.Settings.Default.AutoSaveTimerEnabled = AutoSaveEnabled;

            Properties.Settings.Default.Save();

            Common.ShowMessageBox("Your settings have been saved",
                                  "The changes you've made to these settings have been saved, and will take effect immediately.",
                                  false, false);
            TryClose();
        }

        /// <summary>
        /// Closes the window
        /// </summary>
        public void BtnDone()
        {
            TryClose();
        }
    }
}
