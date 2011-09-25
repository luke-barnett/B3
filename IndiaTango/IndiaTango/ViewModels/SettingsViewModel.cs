using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class SettingsViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;

        private bool _dontNotifyIfFailing = false;
        private bool _formulaValidationAsTyped = false;
        private int _errorThreshold = 0;

        public SettingsViewModel(SimpleContainer container, IWindowManager windowManager)
        { 
            _windowManager = windowManager;
            _container = container;

            // Load in settings
            DontNotifyIfFailing = IndiaTango.Properties.Settings.Default.IgnoreSensorErrorDetection;
            FormulaValidationAsTyped = IndiaTango.Properties.Settings.Default.EvaluateFormulaOnKeyUp;
            ErrorThreshold = IndiaTango.Properties.Settings.Default.DefaultErrorThreshold;
        }

        public string WindowTitle
        {
            get { return "Settings"; }
        }

        public string Title
        {
            get { return "Configure " + Common.ApplicationTitle; }
        }

        public bool DontNotifyIfFailing
        {
            get { return _dontNotifyIfFailing; }
            set { _dontNotifyIfFailing = value; NotifyOfPropertyChange(() => DontNotifyIfFailing); }
        }

        public bool FormulaValidationAsTyped
        {
            get { return _formulaValidationAsTyped; }
            set { _formulaValidationAsTyped = value; NotifyOfPropertyChange(() => FormulaValidationAsTyped); }
        }

        public int ErrorThreshold
        {
            get { return _errorThreshold; }
            set { _errorThreshold = value; NotifyOfPropertyChange(() => ErrorThreshold); }
        }

        public void btnSave()
        {
            IndiaTango.Properties.Settings.Default.DefaultErrorThreshold = ErrorThreshold;
            IndiaTango.Properties.Settings.Default.EvaluateFormulaOnKeyUp = FormulaValidationAsTyped;
            IndiaTango.Properties.Settings.Default.IgnoreSensorErrorDetection = DontNotifyIfFailing;

            IndiaTango.Properties.Settings.Default.Save();

            Common.ShowMessageBox("Your settings have been saved",
                                  "The changes you've made to these settings have been saved, and will take effect immediately.",
                                  false, false);
			this.TryClose();
        }

        public void btnDone()
        {
            this.TryClose();
        }
    }
}
