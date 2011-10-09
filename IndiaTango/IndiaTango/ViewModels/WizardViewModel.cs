using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class WizardViewModel : BaseViewModel
    {
        private SimpleContainer _container;
        private IWindowManager _manager;
        private int _currentStep = 0;

        public WizardViewModel(SimpleContainer container,  IWindowManager manager)
        {
            _container = container;
            _manager = manager;
        }

        public int CurrentStep
        {
            get { return _currentStep; }
            set { _currentStep = value; NotifyOfPropertyChange(() => CurrentStep); NotifyOfPropertyChange(() => CanGoBack); NotifyOfPropertyChange(() => CanGoForward); }
        }

        public string Title
        {
            get { return "Import Wizard"; }
        }

        public string WizardTitle
        {
            get { return "Fix imported data"; }
        }

        public void btnNext()
        {
            CurrentStep = Math.Min(++CurrentStep, 4);
        }

        public void btnBack()
        {
            CurrentStep = Math.Max(--CurrentStep, 0);
        }

        public bool CanGoBack
        {
            get { return CurrentStep > 0; }
        }

        public bool CanGoForward
        {
            get { return CurrentStep < 4; }
        }

        public void btnFinish()
        {
            this.TryClose();
        }
    }
}
