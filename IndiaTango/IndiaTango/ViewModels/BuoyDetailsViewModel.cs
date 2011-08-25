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
        private Buoy _buoy;
    	private Contact _primaryContact;
        private Contact _secondaryContact;
        private Contact _universityContact;
        private ObservableCollection<Buoy> _allBuoys = new ObservableCollection<Buoy>();
		private ObservableCollection<Contact> _allContacts = new ObservableCollection<Contact>();

        public BuoyDetailsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _allBuoys = Buoy.ImportAll(); // TODO: Make this a singleton across whole app?
        	_allContacts = Contact.ImportAll();

			//YUCK YUCK YUCK. We need to store all the contacts externally, 
			//	and perhaps only store contact IDs when we serialize
			foreach (Buoy b in _allBuoys)
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
            get { return "Edit Buoy Details"; }
        }

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
            get { return SelectedBuoy != null; }
        }

		#endregion

		#region ButtonHandlers
        public void btnCancel()
        {
            this.TryClose();
        }

        public void btnUpdate()
        {
            // Buoy exists but has changed - update and re-export
            try
            {
                decimal lat = 0;
                decimal lng = 0;

                if(decimal.TryParse(Latitude, out lat) && decimal.TryParse(Longitude, out lng))
                    SelectedBuoy.GpsLocation = new GPSCoords(lat, lng);
                else
                    SelectedBuoy.GpsLocation = new GPSCoords(Latitude, Longitude);

                SelectedBuoy.Owner = Owner;
                SelectedBuoy.PrimaryContact = PrimaryContact;
                SelectedBuoy.SecondaryContact = SecondaryContact;
                SelectedBuoy.Site = SiteName;
                SelectedBuoy.UniversityContact = UniversityContact;

                Buoy.ExportAll(_allBuoys);
                this.TryClose();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void btnCreate()
        {
            // Brand new buoy; create a new buoy and save it
            Buoy b = null;

            try
            {
                b = new Buoy(Buoy.NextID, SiteName, Owner, PrimaryContact, SecondaryContact, UniversityContact, new GPSCoords(Latitude, Longitude));
                _allBuoys.Add(b);
                Buoy.ExportAll(_allBuoys);
                this.TryClose();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (MessageBox.Show("Are you sure you want to delete this contact?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (PrimaryContact != null)
                {
                    // TODO: consolidate into a single method - too much repetition of code!
                    var allContacts = AllContacts;
                    allContacts.Remove(PrimaryContact);

                    AllContacts = allContacts;
                    PrimaryContact = null;

                    Contact.ExportAll(AllContacts);

                    MessageBox.Show("Contact successfully removed.", "Success", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        public void btnDelete()
        {
            if(MessageBox.Show("Are you sure you want to delete this buoy?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (SelectedBuoy != null)
                {
                    var allBuoys = AllBuoys;
                    allBuoys.Remove(SelectedBuoy);

                    AllBuoys = allBuoys;
                    SelectedBuoy = null;

                    Buoy.ExportAll(AllBuoys);

                    MessageBox.Show("Buoy successfully removed.", "Success", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

#endregion
    }
}
