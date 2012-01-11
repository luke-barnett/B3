using System.Collections.Generic;
using System.Windows;

namespace IndiaTango.ViewModels
{
    class SpecifyValueViewModel : BaseViewModel
    {
        private string _title = "Specify value";
        private List<string> _comboBoxItems;
        private bool _showComboBox;
        private bool _showCancel;
    	private bool _canEditComboBox = true;
        private bool _canceled = true;
        private int _selectedIndex = -1;

        public string Title { get { return _title; } set { _title = value; } }

        private string _text;
        public string Text { get { return _text; } set { _text = value; NotifyOfPropertyChange(()=>Text); } }

        private string _msg = "Please specify a value:";
        public string Message { get { return _msg; } set { _msg = value; NotifyOfPropertyChange(() => Message); } }

        public void BtnOK()
        {
            _canceled = false;
            TryClose();
        }

		public void BtnCancel()
		{
			TryClose();
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
