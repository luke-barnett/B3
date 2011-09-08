using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;

namespace IndiaTango.ViewModels
{
    class SensorTemplateManagerViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;

        public SensorTemplateManagerViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }
    }
}
