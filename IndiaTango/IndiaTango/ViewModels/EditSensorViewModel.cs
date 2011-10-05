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
        private ListedSensor _selectedItem = null;
		private bool _tipVisible = false;
        private bool _errorVisible = false;
		//private List<Sensor> _sensors;
        private List<ListedSensor> _allSensors = new List<ListedSensor>();
    	private bool _editing = false;
        private List<SensorTemplate> Templates = new List<SensorTemplate>();
        private Dataset _ds = null;
        private SummaryType _summaryType;

        public EditSensorViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            Templates = SensorTemplate.ImportAll();
        }

		#region View Properties
        public Dataset Dataset
        {
            get { return _ds; }
            set { _ds = value; }
        }

		public string Title { get { return "Edit Sensor"; } }

        public string Icon { get { return Common.Icon; } }

        public int SummaryMode
        {
            get { return (int) _summaryType; }
            set
            {
                _summaryType = (SummaryType) value;
                NotifyOfPropertyChange(() => SummaryMode);
            }
        }

        public int TipRowHeight
        {
            get { return (TipVisible) ? 45 : 0; }
        }

        public int ErrorRowHeight
        {
            get { return (FailingErrorVisible) ? 60 : 0; }
        }

        public string[] SummaryTypes{get {return new string[]{"Average","Sum"};}}

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

        /*public List<Sensor> Sensors
        {
            get { return _sensors; }
            set { _sensors = value; }
        }*/

		public List<ListedSensor> AllSensors
    	{
			get { return _allSensors; }
			set
			{
                _allSensors = value;
                NotifyOfPropertyChange(() => AllSensors);
			}
    	}

        public ListedSensor SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value; 

                if(_selectedItem != null)
                {
                    Name = _selectedItem.Sensor.Name;
                    Description = _selectedItem.Sensor.Description;
                    LowerLimit = _selectedItem.Sensor.LowerLimit.ToString();
                    UpperLimit = _selectedItem.Sensor.UpperLimit.ToString();
                    Unit = _selectedItem.Sensor.Unit;
                    MaximumRateOfChange = _selectedItem.Sensor.MaxRateOfChange.ToString();
                    Manufacturer = _selectedItem.Sensor.Manufacturer;
                    SerialNumber = _selectedItem.Sensor.SerialNumber;
                    ErrorThreshold = _selectedItem.Sensor.ErrorThreshold.ToString();
                    SummaryMode = (int)_selectedItem.Sensor.SummaryType;
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
                    ErrorThreshold = IndiaTango.Properties.Settings.Default.DefaultErrorThreshold.ToString();
                    SummaryMode = 0;
                }

                FailingErrorVisible = (_selectedItem != null && _selectedItem.IsFailing);
                //var val =
                    //_selectedSensor.CurrentState.GetMissingTimes(Dataset.DataInterval, Dataset.StartTimeStamp, Dataset.EndTimeStamp).Count;

				NotifyOfPropertyChange(() => NeedsTip);

                if(NeedsTip)
                {
                    var gotMatch = false;

                    if(value != null)
                        foreach(SensorTemplate st in Templates)
                            if(st.Matches(value.Sensor))
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
                NotifyOfPropertyChange(() => ErrorThreshold);
                NotifyOfPropertyChange(() => HasSelectedSensor);
            }
        }

        public bool HasSelectedSensor
        {
            get { return SelectedItem != null; }
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
        public string ErrorThreshold { get; set; }
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
            if(SelectedItem == null)
            {
                // New sensor
                try
                {
                    // TODO: more user-friendly conversion messages!
                    var s = new Sensor(Name, Description, float.Parse(UpperLimit), float.Parse(LowerLimit), Unit, float.Parse(MaximumRateOfChange), Manufacturer, SerialNumber, new Stack<SensorState>(), new Stack<SensorState>(), new List<DateTime>(), int.Parse(ErrorThreshold), _ds, SummaryType.Average);
                    SelectedItem = new ListedSensor(s, _ds);
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
                    SelectedItem.Sensor.Name = Name;
                    SelectedItem.Sensor.Description = Description;
                    SelectedItem.Sensor.UpperLimit = float.Parse(UpperLimit);
                    SelectedItem.Sensor.LowerLimit = float.Parse(LowerLimit);
                    SelectedItem.Sensor.Unit = Unit;
                    SelectedItem.Sensor.MaxRateOfChange = float.Parse(MaximumRateOfChange);
                    SelectedItem.Sensor.Manufacturer = Manufacturer;
                    SelectedItem.Sensor.SerialNumber = SerialNumber;
                    SelectedItem.Sensor.ErrorThreshold = int.Parse(ErrorThreshold);
                    SelectedItem.Sensor.SummaryType = (SummaryType)SummaryMode;
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
        	List<ListedSensor> old = AllSensors;
            AllSensors = new List<ListedSensor>();
            AllSensors = old;
        }

		public void btnCancel()
		{
			Editing = false;

			//Force the controls to update
			SelectedItem = SelectedItem;
		}

        public void btnPresets()
        {
            var v = (SensorTemplateManagerViewModel)_container.GetInstance(typeof (SensorTemplateManagerViewModel), "SensorTemplateManagerViewModel");
            v.Sensors = AllSensors;
            v.Deactivated += (o, e) => {
                Templates = SensorTemplate.ImportAll(); /* Update sensor templates after potential change */

                AllSensors = new List<ListedSensor>(); // Refresh list of sensors to cope with change
                AllSensors = v.Sensors;
            };
            _windowManager.ShowDialog(v);
        }

		#endregion
    }
}
