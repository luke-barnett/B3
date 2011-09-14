using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class EditSensorViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        private Sensor _selectedSensor = null;
		private bool _tipVisible = false;
        private bool _errorVisible = false;
		private List<Sensor> _sensors;
    	private bool _editing = false;
        private List<SensorTemplate> Templates = new List<SensorTemplate>(); 

        public EditSensorViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            Templates = SensorTemplate.ImportAll();
        }

		#region View Properties
		public string Title { get { return "Edit Sensor"; } }

        public string Icon { get { return Common.Icon; } }

        public int TipRowHeight
        {
            get { return (TipVisible) ? 45 : 0; }
        }

        public int ErrorRowHeight
        {
            get { return (FailingErrorVisible) ? 60 : 0; }
        }

        public bool FailingErrorVisible
        {
            get { return _errorVisible; }
            set
            {
                _errorVisible = value;

                NotifyOfPropertyChange(() => FailingErrorVisible);
                NotifyOfPropertyChange(() => ErrorRowHeight);
            }
        }

        public bool TipVisible
        {
            get { return _tipVisible; }
            set
            {
                _tipVisible = value;
                
                NotifyOfPropertyChange(() => TipVisible);
                NotifyOfPropertyChange(() => TipRowHeight);
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

                FailingErrorVisible = (_selectedSensor != null && _selectedSensor.IsFailing);

				NotifyOfPropertyChange(() => NeedsTip);

                if(NeedsTip)
                {
                    var gotMatch = false;

                    if(value != null)
                        foreach(SensorTemplate st in Templates)
                            if(st.Matches(value))
                            {
                                Unit = st.Unit;
                                LowerLimit = st.LowerLimit.ToString();
                                UpperLimit = st.UpperLimit.ToString();
                                MaximumRateOfChange = st.MaximumRateOfChange.ToString();
                                gotMatch = true;
                            }

                    TipVisible = gotMatch;
                }
                else
                {
                    TipVisible = false;
                }

				NotifyOfPropertyChange(() => Name);
				NotifyOfPropertyChange(() => Description);
				NotifyOfPropertyChange(() => LowerLimit);
				NotifyOfPropertyChange(() => UpperLimit);
				NotifyOfPropertyChange(() => Unit);
				NotifyOfPropertyChange(() => MaximumRateOfChange);
				NotifyOfPropertyChange(() => Manufacturer);
				NotifyOfPropertyChange(() => SerialNumber);
                NotifyOfPropertyChange(() => HasSelectedSensor);
            }
        }

        public bool HasSelectedSensor
        {
            get { return SelectedSensor != null; }
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
                    EventLogger.LogInfo(GetType().ToString(), "Created new sensor. Sensor name: " + s.Name);
                    this.TryClose();
                }
                catch (Exception e)
                {
                	Common.ShowMessageBox("Format Error", e.Message, false, true);
                    EventLogger.LogWarning(GetType().ToString(), "Attempted to create new sensor, but failed. Details: " + e.Message);
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
                    EventLogger.LogInfo(GetType().ToString(), "Saved existing sensor. Sensor name: " + Name);
                }
                catch (Exception e)
                {
                	Common.ShowMessageBox("An Error Occured", e.Message, false, true);
                    EventLogger.LogWarning(GetType().ToString(), "Attempted to save existing sensor, but failed. Details: " + e.Message);
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

        public void btnPresets()
        {
            var v = (SensorTemplateManagerViewModel)_container.GetInstance(typeof (SensorTemplateManagerViewModel), "SensorTemplateManagerViewModel");
            v.Sensors = _sensors;
            v.Deactivated += (o, e) => {
                Templates = SensorTemplate.ImportAll(); /* Update sensor templates after potential change */ 

            };
            _windowManager.ShowDialog(v);
        }

		#endregion
    }
}
