using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;

namespace IndiaTango.ViewModels
{
    class LoadViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private int _progressvalue;


        public LoadViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public void BtnCancel()
        {

        }

        public  void UpdateProgrssBar(int value)
        {
            ProgressBarValue = value;
        }

        public int ProgressBarValue { get { return _progressvalue; } set { _progressvalue = value; NotifyOfPropertyChange("ProgressBarValue"); } }
    }
}
