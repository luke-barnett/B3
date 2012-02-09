using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class EditSensorViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        private ListedSensor _selectedItem = null;
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

        private Cursor _viewCursor = Cursors.Arrow;

        public Cursor ViewCursor
        {
            get { return _viewCursor; }
            set { _viewCursor = value; NotifyOfPropertyChange(() => ViewCursor); }
        }

        #region Drag and Drop for Sensors
        private ListedSensor _sensorAtStartOfDrag = null;
        private bool isDragging = false;
        private bool movedMouseWhileDragging = false;

        public void StartSensorDrag(SelectionChangedEventArgs e)
        {
            if (isDragging)
                return;

            // This handles the fact that, when you click and drag, this event *only* fires
            // when a *new* item is selected. We want the one from before - hence the
            // use of RemovedItems.

            _sensorAtStartOfDrag = (e.RemovedItems.Count == 1) ? (ListedSensor)e.RemovedItems[0] : (ListedSensor)e.AddedItems[0];

            isDragging = true;
        }

        public void MovedOverSensorList(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ViewCursor = Cursors.SizeAll;
                movedMouseWhileDragging = true;
            }
        }

        public void EndSensorDrag(MouseEventArgs e)
        {
            if (movedMouseWhileDragging)
            {
                isDragging = false;
                var sensorAtEndOfDrag = SelectedItem;

                if(sensorAtEndOfDrag != null && _sensorAtStartOfDrag != null && !sensorAtEndOfDrag.Sensor.Equals(_sensorAtStartOfDrag.Sensor))
                {
                    // Move it into place
                    _ds.MoveSensorTo(_sensorAtStartOfDrag.Sensor, sensorAtEndOfDrag.Sensor);
                    SelectedItem = _sensorAtStartOfDrag;

                    // TODO: must be more efficient way of doing this? Just NotifyPropertyChanged() didn't work
                    var tmpList = AllSensors;
                    AllSensors = new List<ListedSensor>();
                    AllSensors = tmpList;
                }
            }

            _sensorAtStartOfDrag = null;
            movedMouseWhileDragging = false;
            isDragging = false;
            ViewCursor = Cursors.Arrow;
        }
        #endregion

        #region View Properties
        public Dataset Dataset
        {
            get { return _ds; }
            set { _ds = value; }
        }

        public string Title { get { return string.Format("[{0}] Edit Sensors", (Dataset != null ? Dataset.IdentifiableName : Common.UnknownSite)); } }

        public int SummaryType
        {
            get { return (int) _summaryType; }
            set
            {
                _summaryType = (SummaryType) value;
                NotifyOfPropertyChange(() => SummaryType);
            }
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

		public List<ListedSensor> AllSensors
    	{
            get
            {
                return _ds.Sensors.Select(s => new ListedSensor(s, _ds)).ToList();
            }
			set
			{
			    _ds.Sensors = value.Select(s => s.Sensor).ToList();
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
                    Depth = _selectedItem.Sensor.Elevation.ToString();
                    LowerLimit = _selectedItem.Sensor.LowerLimit.ToString();
                    UpperLimit = _selectedItem.Sensor.UpperLimit.ToString();
                    Unit = _selectedItem.Sensor.Unit;
                    MaximumRateOfChange = _selectedItem.Sensor.MaxRateOfChange.ToString();
                    ErrorThreshold = _selectedItem.Sensor.ErrorThreshold.ToString();
                    SummaryType = (int)_selectedItem.Sensor.SummaryType;
                }
                else
                {
                    Name = "";
                    Description = "";
                    Depth = "";
                    LowerLimit = "0";
                    UpperLimit = "0";
                    Unit = "";
                    MaximumRateOfChange = "0";
                    ErrorThreshold = IndiaTango.Properties.Settings.Default.DefaultErrorThreshold.ToString();
                    SummaryType = 0;
                }

                FailingErrorVisible = (_selectedItem != null && _selectedItem.IsFailing);
                //var val =
                    //_selectedSensor.CurrentState.GetMissingTimes(Dataset.DataInterval, Dataset.StartTimeStamp, Dataset.EndTimeStamp).Count;

				NotifyOfPropertyChange(() => Name);
				NotifyOfPropertyChange(() => Description);
                NotifyOfPropertyChange(() => Depth);
				NotifyOfPropertyChange(() => LowerLimit);
				NotifyOfPropertyChange(() => UpperLimit);
				NotifyOfPropertyChange(() => Unit);
				NotifyOfPropertyChange(() => MaximumRateOfChange);
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
                NotifyOfPropertyChange(() => NewVisible);
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
        public string ErrorThreshold { get; set; }
        public string Depth { get; set; }

        public Visibility NewVisible
        {
            get { return (Editing) ? Visibility.Collapsed : Visibility.Visible; }
        }
		#endregion

		#region Event Handlers
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
                    var s = new Sensor(Name, Description, float.Parse(UpperLimit), float.Parse(LowerLimit), Unit, float.Parse(MaximumRateOfChange), new Stack<SensorState>(), new Stack<SensorState>(), new List<Calibration>(), int.Parse(ErrorThreshold), _ds, (SummaryType)SummaryType) { Elevation = float.Parse(Depth)};
                    
                    if(Dataset.Sensors == null)
                        Dataset.Sensors = new List<Sensor>();

                    Dataset.Sensors.Add(s);
                    SelectedItem = new ListedSensor(s, _ds);

                    EventLogger.LogInfo(_ds, GetType().ToString(), "Created new sensor. Sensor name: " + s.Name);
                    Common.ShowMessageBox("New sensor created", "The new sensor '" + Name + "' was added successfully.",
                                          false, false);
                    endEditing();
                }
                catch (Exception e)
                {
                	Common.ShowMessageBox("Format Error", e.Message, false, true);
                    EventLogger.LogWarning(_ds, GetType().ToString(), "Attempted to create new sensor, but failed. Details: " + e.Message);
                }
            }
            else
            {
                // Existing sensor
                try
                {
                    SelectedItem.Sensor.Name = Name;
                    SelectedItem.Sensor.Description = Description;
                    SelectedItem.Sensor.Elevation = float.Parse(Depth);
                    SelectedItem.Sensor.UpperLimit = float.Parse(UpperLimit);
                    SelectedItem.Sensor.LowerLimit = float.Parse(LowerLimit);
                    SelectedItem.Sensor.Unit = Unit;
                    SelectedItem.Sensor.MaxRateOfChange = float.Parse(MaximumRateOfChange);
                    SelectedItem.Sensor.ErrorThreshold = int.Parse(ErrorThreshold);
                    SelectedItem.Sensor.SummaryType = (SummaryType)SummaryType;
                    EventLogger.LogInfo(_ds, GetType().ToString(), "Saved existing sensor. Sensor name: " + Name);
                    endEditing();
                }
                catch (Exception e)
                {
                	Common.ShowMessageBox("An Error Occured", e.Message, false, true);
                    EventLogger.LogWarning(_ds, GetType().ToString(), "Attempted to save existing sensor, but failed. Details: " + e.Message);
                }
            }
        }

        public void btnDelete()
        {
            if(SelectedItem != null && SelectedItem.Sensor != null && Common.Confirm("Really delete sensor?", "Are you sure you want to permanently delete this sensor?"))
            {
                Dataset.Sensors.Remove(SelectedItem.Sensor);
                EventLogger.LogInfo(_ds, GetType().ToString(), "Deleted existing sensor. Sensor name: " + SelectedItem.Sensor.Name);
                Common.ShowMessageBox("Sensor removed", "The selected sensor was successfully deleted.",
                                          false, false);
                endEditing();
            }
        }

        private void endEditing()
        {
            Editing = false;

            //Force the damn list box to update the sensor names
            List<ListedSensor> old = AllSensors;
            AllSensors = new List<ListedSensor>();
            AllSensors = old;
        }

        public void btnNew()
        {
            Editing = true;
            SelectedItem = null;
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
            v.Dataset = Dataset;
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
