using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class SpecifyValueViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private string _title = "Specify value";
        private List<string> _comboBoxItems;
        private bool _showComboBox = false;
    	private bool _showCancel = false;
    	private bool _canEditComboBox = true;
        private bool _canceled = false;
        private int _selectedIndex = -1;

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

		public void btnCancel()
		{
		    _canceled = true;
			this.TryClose();
		}

        public bool ShowComboBox
        {
            get { return _showComboBox; }
            set
            {
                _showComboBox = value;

                NotifyOfPropertyChange(() => ComboBoxVisible);
                NotifyOfPropertyChange(() => TextBoxVisible);
            }
        }

    	public bool ShowCancel
    	{
    		get { return _showCancel; }
			set { _showCancel = value; NotifyOfPropertyChange(() => CancelVisible);}
    	}

        public Visibility ComboBoxVisible
        {
            get { return _showComboBox ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility TextBoxVisible
        {
            get { return _showComboBox ? Visibility.Collapsed : Visibility.Visible; }
        }

        public List<string> ComboBoxItems
        {
            get { return _comboBoxItems; }
            set { _comboBoxItems = value; NotifyOfPropertyChange(() => ComboBoxItems); }
        }

    	public bool CanEditComboBox
    	{
			get { return _canEditComboBox; }
			set { _canEditComboBox = value; NotifyOfPropertyChange(() => CanEditComboBox); }
    	}

        public bool WasCanceled
        {
            get { return _canceled; }
        }

        public Visibility CancelVisible
    	{
    		get { return _showCancel ? Visibility.Visible : Visibility.Collapsed; }
    	}

        public int ComboBoxSelectedIndex
        {
            get { return _selectedIndex; }
            set { _selectedIndex = value; NotifyOfPropertyChange(() => ComboBoxSelectedIndex); }
        }
    }
}
