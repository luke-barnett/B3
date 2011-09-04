using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using System.Windows.Controls;
using MessageBox = System.Windows.Forms.MessageBox;

namespace IndiaTango.ViewModels
{
    class SessionViewModel : BaseViewModel
    {
		#region Private Members
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private List<Sensor> _sensors;
        private BackgroundWorker _bw;

		private static double _progressBarPercent = 0;
		private string _statusLabelText = "To Start, click Import Data, then fill out the properties to the right.";
		private Visibility _progressBarVisible = Visibility.Hidden;
		private bool _actionButtonsEnabled = false;
		private bool _siteControlsEnabled = false;
		private Visibility _createEditDeleteVisible = Visibility.Visible;
		private Visibility _doneCancelVisible = Visibility.Hidden;

		private Buoy _buoy;
		private Contact _primaryContact;
		private Contact _secondaryContact;
		private Contact _universityContact;
		private ObservableCollection<Buoy> _allBuoys = new ObservableCollection<Buoy>();
		private ObservableCollection<Contact> _allContacts = new ObservableCollection<Contact>();

		#endregion

        public SessionViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _sensors = new List<Sensor>();
			_allBuoys = Buoy.ImportAll();
			_allContacts = Contact.ImportAll();

			// TODO: YUCK YUCK YUCK.
			// We need to store all the contacts externally, and perhaps only store contact IDs when we serialize
			foreach (Buoy b in _allBuoys)
			{
				foreach (Contact c in new[] { b.PrimaryContact, b.SecondaryContact, b.UniversityContact })
				{
					if (!_allContacts.Contains(c))
						_allContacts.Add(c);
				}
			}

			//Hack used to force the damn buttons to update
        	DoneCancelVisible = Visibility.Visible;
			DoneCancelVisible = Visibility.Collapsed;
        }



		#region View Properties
		//TODO: Make a gloabl 'editing/creating/viewing site' state that the properties reference

		public string Title
		{
			get { return "New Session"; }
		}

		public double ProgressBarPercent
		{
			get { return _progressBarPercent; }
			set
			{
				_progressBarPercent = value;
				StatusLabelText = LoadingProgressString;

				NotifyOfPropertyChange(() => ProgressBarPercent);
				NotifyOfPropertyChange(() => ProgressBarPercentDouble);
			}
		}

		public double ProgressBarPercentDouble
		{
			get { return ProgressBarPercent / 100; }
		}

		public string ProgressState { get { return ActionButtonsEnabled ? "None" : "Normal"; } }

		public string StatusLabelText
		{
			get { return _statusLabelText; }
			set
			{
				_statusLabelText = value;
				NotifyOfPropertyChange(() => StatusLabelText);
			}
		}

		public Visibility ProgressBarVisible
    	{
			get { return _progressBarVisible; }
			set
			{
				_progressBarVisible = value;
				NotifyOfPropertyChange(()=> ProgressBarVisible);
			}
    	}

		public bool ActionButtonsEnabled
		{
			get { return _actionButtonsEnabled; }
			set
			{
				_actionButtonsEnabled = value;
				NotifyOfPropertyChange(()=> ActionButtonsEnabled);
				NotifyOfPropertyChange(()=> ProgressState);
			}
		}

		public bool EditDeleteEnabled
		{
			get { return SelectedBuoy != null; }
		}

    	public bool SiteListEnabled
    	{
			get { return DoneCancelVisible != Visibility.Visible; }
    	}

		public string LoadingProgressString { get { return string.Format("Loading data: {0}%", ProgressBarPercent); } }

    	public List<Sensor> SensorList
    	{
    		get { return _sensors; } 
			set
			{
				_sensors = value; 
				NotifyOfPropertyChange(() => SensorList);
			}
    	}

		public List<Sensor> SelectedSensor = new List<Sensor>();

		public bool SiteControlsEnabled
    	{
			get { return _siteControlsEnabled; }
			set
			{
				_siteControlsEnabled = value;
				NotifyOfPropertyChange(() => SiteControlsEnabled);
			}
    	}

		public Visibility CreateEditDeleteVisible
    	{
			get { return _createEditDeleteVisible; }
			set 
			{

				_createEditDeleteVisible = value;
				NotifyOfPropertyChange(() => CreateEditDeleteVisible);
			}
    	}

		public Visibility DoneCancelVisible
		{
			get { return _doneCancelVisible; }
			set 
			{
				_doneCancelVisible = value;
				NotifyOfPropertyChange(() => DoneCancelVisible);
				NotifyOfPropertyChange(() => SiteListEnabled);
			}
		}
		#endregion

		#region Site Properties
		public ObservableCollection<Buoy> AllBuoys
		{
			get { return _allBuoys; }
			set { _allBuoys = value; NotifyOfPropertyChange(() => AllBuoys); }
		}

		public ObservableCollection<Contact> AllContacts
		{
			get { return _allContacts; }
			set { _allContacts = value; NotifyOfPropertyChange(() => AllContacts); }
		}

		public string SiteName { get; set; }

		public string Owner { get; set; }

		public string Latitude { get; set; }

		public string Longitude { get; set; }

		public Contact PrimaryContact
		{
			get { return _primaryContact; }
			set
			{
				_primaryContact = value;
				NotifyOfPropertyChange(() => PrimaryContact);
			}
		}

		public Contact SecondaryContact
		{
			get { return _secondaryContact; }
			set
			{
				_secondaryContact = value;
				NotifyOfPropertyChange(() => SecondaryContact);
			}
		}

		public Contact UniversityContact
		{
			get { return _universityContact; }
			set
			{
				_universityContact = value;
				NotifyOfPropertyChange(() => UniversityContact);
			}
		}

		public Buoy SelectedBuoy
		{
			get { return _buoy; }
			set
			{
				_buoy = value;

				if (_buoy != null)
				{
					SiteName = _buoy.Site; // This is all necessary because we create the buoy when we save, not now
					Owner = _buoy.Owner;
					Latitude = _buoy.GpsLocation.DecimalDegreesLatitude.ToString();
					Longitude = _buoy.GpsLocation.DecimalDegreesLongitude.ToString();
					PrimaryContact = _buoy.PrimaryContact;
					SecondaryContact = _buoy.SecondaryContact;
					UniversityContact = _buoy.UniversityContact;
				}
				else
				{
					SiteName = "";
					Owner = "";
					Latitude = "0";
					Longitude = "0";
					PrimaryContact = null;
					SecondaryContact = null;
					UniversityContact = null;
				}

				NotifyOfPropertyChange(() => SelectedBuoy);
				NotifyOfPropertyChange(() => SiteName);
				NotifyOfPropertyChange(() => Owner);
				NotifyOfPropertyChange(() => Latitude);
				NotifyOfPropertyChange(() => Longitude);
				NotifyOfPropertyChange(() => PrimaryContact);
				NotifyOfPropertyChange(() => SecondaryContact);
				NotifyOfPropertyChange(() => UniversityContact);
				NotifyOfPropertyChange(() => EditDeleteEnabled);
			}
		}
		#endregion

		#region Event Handlers
		public void btnImport()
		{
			_bw = new BackgroundWorker();
			_bw.WorkerReportsProgress = true;
			_bw.WorkerSupportsCancellation = true;

			var fileDialog = new OpenFileDialog();
			fileDialog.Filter = "CSV Files|*.csv";
			var result = fileDialog.ShowDialog();
			if (result == DialogResult.OK)
			{
				_bw.DoWork += delegate(object sender, DoWorkEventArgs eventArgs)
				{
					ActionButtonsEnabled = false;
					ProgressBarVisible = Visibility.Visible;

					var reader = new CSVReader(fileDialog.FileName);
					reader.ProgressChanged += delegate(object o, ReaderProgressChangedArgs e)
					{
						ProgressBarPercent = e.Progress;
					};

					SensorList = reader.ReadSensors(_bw);

					if (SensorList == null)
					{
						eventArgs.Cancel = true;
						ProgressBarPercent = 0;
					}

					ProgressBarVisible = Visibility.Hidden;
					ActionButtonsEnabled = true;
					StatusLabelText = "";
				};
				_bw.RunWorkerAsync();
			}
		}

		public void btnGraph()
		{
			var graphView = (_container.GetInstance(typeof(GraphViewModel), "GraphViewModel") as GraphViewModel);
			graphView.SensorList = SensorList;
			_windowManager.ShowWindow(graphView);
		}

		public void btnSave()
		{
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnExport()
		{
			//TODO: Need to make dataset return startTime and endTime dynamilly depending on data
			//Buoy buoy;// = new Buoy();
			//Dataset dataSet = new Dataset(buoy,);
			//DatasetExporter exporter = new DatasetExporter(dataSet);
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnOutOfRangeValues()
		{
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnMissingValues()
		{
			var MissingValuesView =
				(_container.GetInstance(typeof(MissingValuesViewModel), "MissingValuesViewModel") as MissingValuesViewModel);
			MissingValuesView.SensorList = SensorList;
			_windowManager.ShowDialog(MissingValuesView);
		}

		public void btnEditPoints()
		{
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnSiteCreate()
		{
			CreateEditDeleteVisible = Visibility.Collapsed;
			DoneCancelVisible = Visibility.Visible;
			SiteControlsEnabled = true;

			SelectedBuoy = null;
		}

		public void btnSiteEdit()
		{
			CreateEditDeleteVisible = Visibility.Collapsed;
			DoneCancelVisible = Visibility.Visible;
			SiteControlsEnabled = true;
		}

		public void btnSiteDelete()
		{
			if (SelectedBuoy != null)
			{
				if(MessageBox.Show("Are you sure you want to delete this site?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
				{
                    var allBuoys = AllBuoys;
                    allBuoys.Remove(SelectedBuoy);

                    AllBuoys = allBuoys;
                    SelectedBuoy = null;

                    Buoy.ExportAll(AllBuoys);

                    MessageBox.Show("Site successfully removed.", "Success", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
		}

		public void btnSiteDone()
		{
			//TODO:Live sanity checking on fields
			try
			{
				//If saving an existing buoy
				if (SelectedBuoy != null)
				{
					decimal lat = 0;
					decimal lng = 0;

					if (decimal.TryParse(Latitude, out lat) && decimal.TryParse(Longitude, out lng))
						SelectedBuoy.GpsLocation = new GPSCoords(lat, lng);
					else
						SelectedBuoy.GpsLocation = new GPSCoords(Latitude, Longitude);

					SelectedBuoy.Owner = Owner;
					SelectedBuoy.PrimaryContact = PrimaryContact;
					SelectedBuoy.SecondaryContact = SecondaryContact;
					SelectedBuoy.Site = SiteName;
					SelectedBuoy.UniversityContact = UniversityContact;
				}
				//else if creating a new one
				else
				{
					Buoy b = new Buoy(Buoy.NextID, SiteName, Owner, PrimaryContact, SecondaryContact, UniversityContact, new GPSCoords(Latitude, Longitude));
					_allBuoys.Add(b);
					Buoy.ExportAll(_allBuoys);
					SelectedBuoy = b;
				}

				Buoy.ExportAll(_allBuoys);

				CreateEditDeleteVisible = Visibility.Visible;
				DoneCancelVisible = Visibility.Collapsed;
				SiteControlsEnabled = false;

				//TODO: List box of bouys does not update properly
			}
			catch (Exception e)
			{
				//TODO: Be more informative
				Common.ShowMessageBox("Bouy Details Error", e.Message, false, true);
			}
		}

		public void btnSiteCancel()
		{
			SelectedBuoy = SelectedBuoy;

			CreateEditDeleteVisible = Visibility.Visible;
			DoneCancelVisible = Visibility.Collapsed;
			SiteControlsEnabled = false;
		}

		public void btnSensors()
		{
			var editSensor =
					_container.GetInstance(typeof(EditSensorViewModel), "EditSensorViewModel") as EditSensorViewModel;
			editSensor.Sensors = SensorList;

			_windowManager.ShowWindow(editSensor);
		}

		public void btnCancel()
		{
			if (_bw != null)
			{
				try
				{
					_bw.CancelAsync();
					ActionButtonsEnabled = true;
				}
				catch (InvalidOperationException ex)
				{
					// TODO: meaningful error here
					System.Diagnostics.Debug.WriteLine("Cannot cancel data loading thread - " + ex);
				}
			}
		}

		public void SelectionChanged(SelectionChangedEventArgs e)
		{
			foreach (Sensor item in e.RemovedItems)
			{
				SelectedSensor.Remove(item);
			}

			foreach (Sensor item in e.AddedItems)
			{
				SelectedSensor.Add(item);
			}
		}

		#endregion
    }
}
