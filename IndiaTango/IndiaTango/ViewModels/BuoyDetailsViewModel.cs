using System;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class BuoyDetailsViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private Buoy _buoy;

        public BuoyDetailsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
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
    }
}
