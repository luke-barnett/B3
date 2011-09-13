using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class SpecifyValueViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private string _title = "Specify value";

        public SpecifyValueViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;
        }

        public string Title { get { return _title; } set { _title = value; } }

        private string _text = null;
        public string Text { get { return _text; } set { _text = value; NotifyOfPropertyChange(()=>Text); } }

        private string _msg = "Please specify a value:";
        public string Message { get { return _msg; } set { _msg = value; NotifyOfPropertyChange(() => Message); } }

        public void btnOK()
        {
            this.TryClose();
        }
    }
}
