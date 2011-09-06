using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class BuoyDetailsViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private Site _site;
    	private Contact _primaryContact;
        private Contact _secondaryContact;
        private Contact _universityContact;
        private ObservableCollection<Site> _allBuoys = new ObservableCollection<Site>();
		private ObservableCollection<Contact> _allContacts = new ObservableCollection<Contact>();

        public BuoyDetailsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _allBuoys = Site.ImportAll(); // TODO: Make this a singleton across whole app?
        	_allContacts = Contact.ImportAll();

			//YUCK YUCK YUCK. We need to store all the contacts externally, 
			//	and perhaps only store contact IDs when we serialize
			foreach (Site b in _allBuoys)
			{
				foreach (Contact c in new[]{b.PrimaryContact,b.SecondaryContact,b.UniversityContact})
				{
					if(!_allContacts.Contains(c))
						_allContacts.Add(c);
				}
        	}
        }

		#region Properties
        public string Title
        {
            get { return "Edit Site Details"; }
        }

        public ObservableCollection<Site> AllBuoys
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
				NotifyOfPropertyChange(()=> PrimaryContact);
				NotifyOfPropertyChange(() => CanEditPrimary);
			}
    	}

        public Contact SecondaryContact
        {
            get { return _secondaryContact; }
            set
            {
                _secondaryContact = value;
                NotifyOfPropertyChange(() => SecondaryContact);
                NotifyOfPropertyChange(() => CanEditSecondary);
            }
        }

        public Contact UniversityContact
        {
            get { return _universityContact; }
            set
            {
                _universityContact = value;
                NotifyOfPropertyChange(() => UniversityContact);
                NotifyOfPropertyChange(() => CanEditUni);
            }
        }

        public Site SelectedSite
        {
            get { return _site; }
            set
            {
                _site = value;

                if (_site != null)
                {
                    SiteName = _site.Name; // This is all necessary because we create the Site when we save, not now
                    Owner = _site.Owner;
                    Latitude = _site.GpsLocation.DecimalDegreesLatitude.ToString();
                    Longitude = _site.GpsLocation.DecimalDegreesLongitude.ToString();
                    PrimaryContact = _site.PrimaryContact;
                    SecondaryContact = _site.SecondaryContact;
                    UniversityContact = _site.UniversityContact;
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
                NotifyOfPropertyChange(() => CanOverwrite);
            }
        }

		public bool CanEditPrimary
    	{
			get { return PrimaryContact != null; }
    	}

        public bool CanEditSecondary
    	{
			get { return SecondaryContact != null; }
    	}
        
        public bool CanEditUni
    	{
			get { return UniversityContact != null; }
    	}

		public bool CanOverwrite
        {
            get { return SelectedSite != null; }
        }

		#endregion

		#region ButtonHandlers
        public void btnCancel()
        {
            this.TryClose();
        }

        public void btnUpdate()
        {
            // Site exists but has changed - update and re-export
            try
            {
                decimal lat = 0;
                decimal lng = 0;

                if(decimal.TryParse(Latitude, out lat) && decimal.TryParse(Longitude, out lng))
                    SelectedSite.GpsLocation = new GPSCoords(lat, lng);
                else
                    SelectedSite.GpsLocation = new GPSCoords(Latitude, Longitude);

                SelectedSite.Owner = Owner;
                SelectedSite.PrimaryContact = PrimaryContact;
                SelectedSite.SecondaryContact = SecondaryContact;
                SelectedSite.Name = SiteName;
                SelectedSite.UniversityContact = UniversityContact;

                Site.ExportAll(_allBuoys);
                this.TryClose();
            }
            catch (Exception e)
            {
                Common.ShowMessageBox("Error", e.Message, false, true);
            }
        }

        public void btnCreate()
        {
            // Brand new Site; create a new Site and save it
            Site b = null;

            try
            {
                b = new Site(Site.NextID, SiteName, Owner, PrimaryContact, SecondaryContact, UniversityContact, new GPSCoords(Latitude, Longitude));
                _allBuoys.Add(b);
                Site.ExportAll(_allBuoys);
                this.TryClose();
            }
            catch (Exception e)
            {
                Common.ShowMessageBox("Error", e.Message, false, true);
            }
        }

        // TODO: make this tidier...
        public void btnNewPrimary()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = null;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        public void btnNewSecondary()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = null;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        public void btnNewUni()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = null;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        public void btnEditPrimary()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = PrimaryContact;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        public void btnEditSecondary()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = SecondaryContact;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        public void btnEditUni()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = UniversityContact;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        public void btnDelPrimary()
        {
            if (Common.Confirm("Confirm Delete", "Are you sure you want to delete this contact?"))
            {
                if (PrimaryContact != null)
                {
                    // TODO: consolidate into a single method - too much repetition of code!
                    var allContacts = AllContacts;
                    allContacts.Remove(PrimaryContact);

                    AllContacts = allContacts;
                    PrimaryContact = null;

                    Contact.ExportAll(AllContacts);

                    Common.ShowMessageBox("Success", "Contact successfully removed.", false, false);
                }
            }
        }

        public void btnDelete()
        {
            if(Common.Confirm("Confirm Delete", "Are you sure you want to delete this site?"))
            {
                if (SelectedSite != null)
                {
                    var allBuoys = AllBuoys;
                    allBuoys.Remove(SelectedSite);

                    AllBuoys = allBuoys;
                    SelectedSite = null;

                    Site.ExportAll(AllBuoys);

                    Common.ShowMessageBox("Success", "Site successfully removed.", false, false);
                }
            }
        }

#endregion
    }
}
