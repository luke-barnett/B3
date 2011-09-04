using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using MessageBox = System.Windows.Forms.MessageBox;

namespace IndiaTango.ViewModels
{
    class EditSensorViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        private Sensor _selectedSensor = new Sensor();
		private bool _tipVisible = false;
		private List<Sensor> _sensors;
    	private bool _editing = false;

        public EditSensorViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

		#region View Properties
		public string Title { get { return "Edit Sensor"; } }

        public string Icon { get { return Common.Icon; } }

        public string TipVisibleEnum
        {
            get { return (TipVisible) ? "Visible" : "Collapsed"; }
        }

        public int TipRowHeight
        {
            get { return (TipVisible) ? 45 : 0; }
        }

        public bool TipVisible
        {
            get { return _tipVisible; }
            set
            {
                _tipVisible = value;
                
                NotifyOfPropertyChange(() => TipVisible);
            }
        }

        public bool NeedsTip
        {
            get { return String.IsNullOrEmpty(Unit); }
        }

		public List<Sensor> Sensors
    	{
			get { return _sensors; }
			set
			{
				_sensors = value;
				NotifyOfPropertyChange(() => Sensors);
			}
    	}

        public Sensor SelectedSensor
        {
            get { return _selectedSensor; }
            set
            {
                _selectedSensor = value; 

                if(_selectedSensor != null)
                {
                    Name = _selectedSensor.Name;
                    Description = _selectedSensor.Description;
                    LowerLimit = _selectedSensor.LowerLimit.ToString();
                    UpperLimit = _selectedSensor.UpperLimit.ToString();
                    Unit = _selectedSensor.Unit;
                    MaximumRateOfChange = _selectedSensor.MaxRateOfChange.ToString();
                    Manufacturer = _selectedSensor.Manufacturer;
                    SerialNumber = _selectedSensor.SerialNumber;
                }
                else
                {
                    Name = "";
                    Description = "";
                    LowerLimit = "0";
                    UpperLimit = "0";
                    Unit = "";
                    MaximumRateOfChange = "0";
                    Manufacturer = "";
                    SerialNumber = "";
                }

				NotifyOfPropertyChange(() => NeedsTip);

                if(NeedsTip)
                {
                    // Provide defaults based on name
                    if(Name.Contains("Temp"))
                    {
                        // TODO: load these from presets on disk
                        Unit = "°C";
                        LowerLimit = "-15";
                        UpperLimit = "38";
                        MaximumRateOfChange = "9";
                        TipVisible = true;
                    }
                }

				NotifyOfPropertyChange(() => Name);
				NotifyOfPropertyChange(() => Description);
				NotifyOfPropertyChange(() => LowerLimit);
				NotifyOfPropertyChange(() => UpperLimit);
				NotifyOfPropertyChange(() => Unit);
				NotifyOfPropertyChange(() => MaximumRateOfChange);
				NotifyOfPropertyChange(() => Manufacturer);
				NotifyOfPropertyChange(() => SerialNumber);
				
            }
        }

    	public bool Editing
    	{
			get { return _editing; }
			set
			{
				_editing = value;
				NotifyOfPropertyChange(() => SaveCancelVisible);
				NotifyOfPropertyChange(() => EditDoneVisible);
				NotifyOfPropertyChange(() => ListEnabled);
				NotifyOfPropertyChange(() => Editing);
			}
    	}

    	public Visibility SaveCancelVisible
    	{
			get { return _editing ? Visibility.Visible : Visibility.Collapsed; }
    	}

		public Visibility EditDoneVisible
		{
			get { return _editing ? Visibility.Collapsed : Visibility.Visible; }
		}

    	public bool ListEnabled
    	{
			get { return !Editing; }
    	}

		#endregion

		
		#region Sensor Properties
        public string Name { get; set; }
        public string Description { get; set; }
        public string LowerLimit { get; set; }
        public string UpperLimit { get; set; }
        public string Unit { get; set; }
        public string MaximumRateOfChange { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
		#endregion


		#region Event Handlers
        
		public void SelectionChanged(SelectionChangedEventArgs e)
		{
			//if(e.AddedItems.Count > 0)
			//	SelectedSensor = (Sensor)e.AddedItems[0];

			//MessageBox.Show(((Sensor)e.AddedItems[0]).ToString());
		}

		public void btnEdit()
		{
			Editing = true;
		}

		public void btnDone()
		{
			this.TryClose();
		}

        public void btnSave()
        {
            if(SelectedSensor == null)
            {
                // New sensor
                try
                {
                    // TODO: more user-friendly conversion messages!
                    Sensor s = new Sensor(Name, Description, float.Parse(UpperLimit), float.Parse(LowerLimit), Unit, float.Parse(MaximumRateOfChange), Manufacturer, SerialNumber);
                    SelectedSensor = s;
                    this.TryClose();
                }
                catch (Exception e)
                {
                	Common.ShowMessageBox("Format Error", e.Message, false, true);
                }
            }
            else
            {
                // Existing sensor
                try
                {
                    SelectedSensor.Name = Name;
                    SelectedSensor.Description = Description;
                    SelectedSensor.UpperLimit = float.Parse(UpperLimit);
                    SelectedSensor.LowerLimit = float.Parse(LowerLimit);
                    SelectedSensor.Unit = Unit;
                    SelectedSensor.MaxRateOfChange = float.Parse(MaximumRateOfChange);
                    SelectedSensor.Manufacturer = Manufacturer;
                    SelectedSensor.SerialNumber = SerialNumber;
                }
                catch (Exception e)
                {
                	Common.ShowMessageBox("An Error Occured", e.Message, false, true);
                }
            }

        	Editing = false;

			//Force the damn list box to update the sensor names
        	List<Sensor> old = Sensors;
			Sensors = new List<Sensor>();
        	Sensors = old;
        }

		public void btnCancel()
		{
			Editing = false;

			//Force the controls to update
			SelectedSensor = SelectedSensor;
		}

		#endregion
    }
}
