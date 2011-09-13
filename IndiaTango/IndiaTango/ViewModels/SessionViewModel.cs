using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
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
        private Dataset _ds;
        private BackgroundWorker _bw;

		private static double _progressBarPercent = 0;

        private const string STATUS_TEXT_AWAITING_INPUT =
            "To Start, click Import Data, then fill out the properties to the right.";

        private string _statusLabelText = STATUS_TEXT_AWAITING_INPUT;
		private Visibility _progressBarVisible = Visibility.Hidden;
		private bool _actionButtonsEnabled = false;
		private bool _siteControlsEnabled = false;
		private Visibility _createEditDeleteVisible = Visibility.Visible;
		private Visibility _doneCancelVisible = Visibility.Hidden;

		private Contact _primaryContact;
		private Contact _secondaryContact;
		private Contact _universityContact;
		private ObservableCollection<Site> _allSites = new ObservableCollection<Site>();
		private ObservableCollection<Contact> _allContacts = new ObservableCollection<Contact>();

		#endregion

        public SessionViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _ds = new Dataset(null);
			_allSites = Site.ImportAll();
			_allContacts = Contact.ImportAll();

			//Hack used to force the damn buttons to update
        	DoneCancelVisible = Visibility.Visible;
			DoneCancelVisible = Visibility.Collapsed;
        }

		#region View Properties
		//TODO: Make a gloabl 'editing/creating/viewing site' state that the properties reference
        private Visibility _sensorWarningVis = Visibility.Collapsed;

        public Visibility SensorWarningVisible
        {
            get { return _sensorWarningVis; }
            set { _sensorWarningVis = value; NotifyOfPropertyChange(() => SensorWarningVisible); }
        }

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
			get { return SelectedSite != null; }
		}

    	public bool SiteListEnabled
    	{
			get { return DoneCancelVisible != Visibility.Visible; }
    	}

		public string LoadingProgressString { get { return string.Format("Loading data: {0}%", ProgressBarPercent); } }

    	public List<Sensor> SensorList
    	{
            get { return _ds.Sensors; }
			set
			{
			    _ds.Sensors = value;
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

        private bool _importEnabled = true;
        public bool ImportEnabled { get { return _importEnabled; } set { _importEnabled = value; NotifyOfPropertyChange(() => ImportEnabled); NotifyOfPropertyChange(() => ShowImportCancel); } }

        public bool HasSelectedPrimaryContact
        {
            get { return PrimaryContact != null; }
        }

        public bool HasSelectedSecondaryContact
        {
            get { return SecondaryContact != null; }
        }

        public bool HasSelectedUniContact
        {
            get { return UniversityContact != null; }
        }

        public Visibility ShowImportCancel
        {
            get { return (ImportEnabled) ? Visibility.Collapsed : Visibility.Visible; }
        }
		#endregion

		#region Site Properties
		public ObservableCollection<Site> AllSites
		{
			get { return _allSites; }
            set { _allSites = value; NotifyOfPropertyChange(() => AllSites); }
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
                NotifyOfPropertyChange(() => HasSelectedPrimaryContact);
			}
		}

		public Contact SecondaryContact
		{
			get { return _secondaryContact; }
			set
			{
				_secondaryContact = value;
				NotifyOfPropertyChange(() => SecondaryContact);
                NotifyOfPropertyChange(() => HasSelectedSecondaryContact);
			}
		}

		public Contact UniversityContact
		{
			get { return _universityContact; }
			set
			{
				_universityContact = value;
				NotifyOfPropertyChange(() => UniversityContact);
                NotifyOfPropertyChange(() => HasSelectedUniContact);
			}
		}

		public Site SelectedSite
		{
            get { return _ds.Site; }
			set
			{
			    _ds.Site = value;
                if(_ds.Site != null)
				{
                    SiteName = _ds.Site.Name; // This is all necessary because we create the Site when we save, not now
                    Owner = _ds.Site.Owner;
                    Latitude = _ds.Site.GpsLocation.DecimalDegreesLatitude.ToString();
                    Longitude = _ds.Site.GpsLocation.DecimalDegreesLongitude.ToString();
                    PrimaryContact = _ds.Site.PrimaryContact;
                    SecondaryContact = _ds.Site.SecondaryContact;
                    UniversityContact = _ds.Site.UniversityContact;
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

				NotifyOfPropertyChange(() => SelectedSite);
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
        public void OnLoaded()
        {
            EventLogger.LogInfo(GetType().ToString(), "Session loaded.");
        }

        public void OnUnloaded()
        {
            EventLogger.LogInfo(GetType().ToString(), "Session closed.");
        }

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
                    EventLogger.LogInfo("BackgroundImportThread", "Data import started.");
					ActionButtonsEnabled = false;
					ProgressBarVisible = Visibility.Visible;

					var reader = new CSVReader(fileDialog.FileName);

					reader.ProgressChanged += ImportProgressChanged;

				    var readSensors = reader.ReadSensors(_bw);  // Prevent null references

					if (readSensors == null)
					{
						eventArgs.Cancel = true;
						ProgressBarPercent = 0;
					    StatusLabelText = STATUS_TEXT_AWAITING_INPUT;
					    ActionButtonsEnabled = false;
					}
					else
					{
                        // Loaded successfully
                        ActionButtonsEnabled = true;
                        StatusLabelText = "";
					    SensorList = readSensors;

                        foreach(var s in readSensors)
                            if(s.IsFailing)
                            {
                                SensorWarningVisible = Visibility.Visible;
                                break;
                            }
					}

                    ImportEnabled = true;
                    ProgressBarVisible = Visibility.Hidden;
                    EventLogger.LogInfo("BackgroundImportThread", "Data import complete.");
				};

                ImportEnabled = false;
			    
				_bw.RunWorkerAsync();
			}
		}

		public void ImportProgressChanged(object o, ReaderProgressChangedArgs e)
		{
			ProgressBarPercent = e.Progress;
		}

		public void btnGraph()
		{
			var graphView = (_container.GetInstance(typeof(GraphViewModel), "GraphViewModel") as GraphViewModel);
			graphView.SensorList = SensorList;
			_windowManager.ShowWindow(graphView);
		}

		public void btnSave()
		{
            EventLogger.LogInfo(GetType().ToString(), "Session save started.");
			Common.ShowFeatureNotImplementedMessageBox();
            EventLogger.LogInfo(GetType().ToString(), "Session save complete. File saved to:");
		}

		public void btnExport()
		{
            var exportWindow =
                    _container.GetInstance(typeof(ExportViewModel), "ExportViewModel") as ExportViewModel;
            exportWindow.Dataset = _ds;

            _windowManager.ShowDialog(exportWindow);
		}

		public void btnOutOfRangeValues()
		{
			Common.ShowFeatureNotImplementedMessageBox();
		}

		public void btnMissingValues()
		{
			var MissingValuesView =
                (_container.GetInstance(typeof(MissingValuesViewModel), "MissingValuesViewModel") as MissingValuesViewModel);
            MissingValuesView.Dataset = _ds;
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

			SelectedSite = null;
		}

		public void btnSiteEdit()
		{
			CreateEditDeleteVisible = Visibility.Collapsed;
			DoneCancelVisible = Visibility.Visible;
			SiteControlsEnabled = true;
		}

		public void btnSiteDelete()
		{
			if (SelectedSite != null)
			{
                if (Common.Confirm("Confirm Delete", "Are you sure you want to delete this site?"))
				{
                    EventLogger.LogInfo(GetType().ToString(), "Site deleted. Site name: " + SelectedSite.Name);

                    var allSites = AllSites;
                    allSites.Remove(SelectedSite);

                    AllSites = allSites;
                    SelectedSite = null;

                    Site.ExportAll(AllSites);

				    Common.ShowMessageBox("Site Management", "Site successfully removed.", false, false);
				}
            }
		}

		public void btnSiteDone()
		{
			//TODO:Live sanity checking on fields
			try
			{
				//If saving an existing Site
				if (SelectedSite != null)
				{
				    SelectedSite.GpsLocation = GPSCoords.Parse(Latitude, Longitude);

					SelectedSite.Owner = Owner;
					SelectedSite.PrimaryContact = PrimaryContact;
					SelectedSite.SecondaryContact = SecondaryContact;
                    SelectedSite.Name = SiteName;
					SelectedSite.UniversityContact = UniversityContact;
                    EventLogger.LogInfo(GetType().ToString(), "Site saved. Site name: " + SelectedSite.Name);
				}
				//else if creating a new one
				else
				{
					Site b = new Site(Site.NextID, SiteName, Owner, PrimaryContact, SecondaryContact, UniversityContact, GPSCoords.Parse(Latitude, Longitude));
					_allSites.Add(b);
					Site.ExportAll(_allSites);
					SelectedSite = b;
                    EventLogger.LogInfo(GetType().ToString(), "Site created. Site name: " + SelectedSite.Name);
				}

				Site.ExportAll(_allSites);

				CreateEditDeleteVisible = Visibility.Visible;
				DoneCancelVisible = Visibility.Collapsed;
				SiteControlsEnabled = false;
			}
			catch (Exception e)
			{
				Common.ShowMessageBox("Error", e.Message, false, true);
			    EventLogger.LogError(GetType().ToString(), "Tried to create site but failed. Details: " + e.Message);
			}
		}

		public void btnSiteCancel()
		{
			SelectedSite = SelectedSite;

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
                    EventLogger.LogWarning(GetType().ToString(), "Data import canceled by user.");
				}
				catch (InvalidOperationException ex)
				{
				    Common.ShowMessageBox("Error", "You cannot cancel the loading of this data set at this time. Try again later.",
				                          false, true);
					System.Diagnostics.Debug.WriteLine("Cannot cancel data loading thread - " + ex);
                    EventLogger.LogError(GetType().ToString(), "Data import could not be canceled.");
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

        #region Contact Add/Edit/Delete Handlers
        public void btnNewPrimary()
        {
            NewContact();
        }

        public void btnNewSecondary()
        {
            NewContact();
        }

        public void btnNewUni()
        {
            NewContact();   
        }

        public void btnEditPrimary()
        {
            EditContact(PrimaryContact);
        }

        public void btnEditSecondary()
        {
            EditContact(SecondaryContact);
        }

        public void btnEditUni()
        {
            EditContact(UniversityContact);
        }

        public void btnDelPrimary()
        {
            DeleteContact(PrimaryContact);
        }

        public void btnDelSecondary()
        {
            DeleteContact(SecondaryContact);
        }

        public void btnDelUni()
        {
            DeleteContact(UniversityContact);
        }

        private void DeleteContact(Contact c)
        {
            if (Common.Confirm("Confirm Delete", "Are you sure you want to delete this contact?"))
            {
                if (c != null)
                {
                    var allContacts = AllContacts;
                    allContacts.Remove(c);

                    AllContacts = allContacts;
                    c = null;

                    Contact.ExportAll(AllContacts);

                    Common.ShowMessageBox("Success", "Contact successfully removed.", false, false);
                }
            }
        }

        private void EditContact(Contact c)
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = c;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        private void NewContact()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = null;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }
		#endregion
    }
}
