using System;
using System.Collections.Generic;
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
        private List<Buoy> _allBuoys = new List<Buoy>();
        private Buoy _selected = null;

        public BuoyDetailsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _allBuoys = Buoy.ImportAll(); // TODO: Make this a singleton across whole app?
        }

        public string Title
        {
            get { return "Edit Buoy Details"; }
        }

        public List<Buoy> AllBuoys
        {
            get { return _allBuoys; }
        }

        private void CreateNewBuoyIfNeeded()
        {
            if(_buoy == null) // TODO: make this nicer!
                _buoy = new Buoy(Buoy.NextID, "Unspecified", "Unspecified", new Contact("", "", "test@test.com", "", ""), new Contact("", "", "test@test.com", "", ""), new Contact("", "", "test@test.com", "", ""), new GPSCoords(0, 0));
        }

        public string SiteName
        {
            get { return (_buoy != null) ? _buoy.Site : ""; }
            set
            {
                CreateNewBuoyIfNeeded(); _buoy.Site = value; 
            }
        }

        public string Owner
        {
            get { return (_buoy != null) ? _buoy.Owner : ""; }
            set
            {
                CreateNewBuoyIfNeeded(); _buoy.Owner = value;
            }
        }

        public string Latitude
        {
            get { return (_buoy != null) ? _buoy.GpsLocation.DecimalDegreesLatitude.ToString() : ""; }
            set
            {
                CreateNewBuoyIfNeeded(); _buoy.GpsLocation.DecimalDegreesLatitude = Convert.ToDecimal(value);
            }
        }

        public string Longitude
        {
            get { return (_buoy != null) ? _buoy.GpsLocation.DecimalDegreesLongitude.ToString() : ""; }
            set
            {
                CreateNewBuoyIfNeeded(); _buoy.GpsLocation.DecimalDegreesLongitude = Convert.ToDecimal(value);
            }
        }

        public Buoy SelectedBuoy
        {
            get { CreateNewBuoyIfNeeded(); return _buoy; }
            set
            {
                _buoy = value;

                NotifyOfPropertyChange(() => SelectedBuoy);
                NotifyOfPropertyChange(() => SiteName);
                NotifyOfPropertyChange(() => Owner);
                NotifyOfPropertyChange(() => Latitude);
                NotifyOfPropertyChange(() => Longitude);
            }
        }

        public void btnCancel()
        {
            this.TryClose();
        }

        public void btnSave()
        {
            // Save state
        }

        public void btnChoosePrimary()
        {
            var editor =
                _container.GetInstance(typeof (ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;
            _windowManager.ShowDialog(editor);
        }
    }
}
