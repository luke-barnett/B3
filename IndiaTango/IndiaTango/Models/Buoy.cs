using System;
using System.Collections.Generic;

namespace IndiaTango.Models
{
    public class Buoy
    {
        private int _iD;
        private string _site;
        private string _owner;
        private Contact _primaryContact;
        private Contact _secondaryContact;
        private GPSCoords _gpsLocation;

        /// <summary>
        /// Creates a new buoy.
        /// </summary>
        /// <param name="iD">The unique ID field of the buoy</param>
        /// <param name="site">The name of the site of the buoy</param>
        /// <param name="owner">The name of the owner of the buoy</param>
        /// <param name="primaryContact">The details of the primary contact</param>
        /// <param name="secondaryContact">The details of the secondary contact</param>
        /// <param name="universityContact">The details of the optional university contact</param>
        /// <param name="gpsLocation">The GPS coordinates of the buoy</param>
        public Buoy(int iD, string site, string owner, Contact primaryContact, Contact secondaryContact, Contact universityContact, GPSCoords gpsLocation)
        {
            if(iD<0)
                throw new ArgumentException("ID number must be greater than 1");
            if(String.IsNullOrEmpty(site))
                throw new ArgumentException("Site must not be empty");
            if(String.IsNullOrEmpty(owner))
                throw new ArgumentException("Owner must not be empty");
            if(primaryContact == null)
                throw new ArgumentException("Primary contact must not be null");
            if(secondaryContact == null)
                throw new ArgumentException("Secondary contact must not be null");
            if(gpsLocation == null)
                throw new ArgumentException("GPS Location must be supplied");
            _iD = iD;
            _site = site;
            _owner = owner;
            _primaryContact = primaryContact;
            _secondaryContact = secondaryContact;
            UniversityContact = universityContact;
            _gpsLocation = gpsLocation;
            Events = new List<Event>();
        }

        #region Public variables
        /// <summary>
        /// Sets and gets the ID of the buoy
        /// </summary>
        public int Id
        {
            get { return _iD; }
            set
            {
                if(value < 0)
                    throw new FormatException("ID number must be greater than 1");
                _iD = value;
            }
        }

        /// <summary>
        /// Sets and gets the site name of the buoy
        /// </summary>
        public string Site
        {
            get { return _site; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new FormatException("Site must not be empty");
                _site = value;
            }
        }

        /// <summary>
        /// Sets and gets the owner of the buoy
        /// </summary>
        public string Owner
        {
            get { return _owner; }
            set
            {
                if(String.IsNullOrEmpty(value))
                    throw new FormatException("Owner must not be empty");
                _owner = value;
            }
        }

        /// <summary>
        /// Sets and gets the details of the primary contact for this buoy
        /// </summary>
        public Contact PrimaryContact
        {
            get { return _primaryContact; }
            set
            {
                if(value == null)
                    throw new FormatException("Primary contact must not be null");
                _primaryContact = value;
            }
        }

        /// <summary>
        /// Sets and gets the details of the secondary contact for this buoy
        /// </summary>
        public Contact SecondaryContact
        {
            get { return _secondaryContact; }
            set
            {
                if(value == null)
                    throw new FormatException("Secondary contact must not be null");
                _secondaryContact = value;
            }
        }

        /// <summary>
        /// Sets and gets the details of the university contact for this buoy
        /// </summary>
        public Contact UniversityContact { get; set; }
        
        /// <summary>
        /// Gets the list of events for this buoy
        /// </summary>
        public List<Event> Events { get; private set; }

        /// <summary>
        /// Sets and gets the GPS location of this buoy
        /// </summary>
        public GPSCoords GpsLocation
        {
            get { return _gpsLocation; }
            set
            {
                if(value == null)
                    throw new FormatException("GPS Location must be supplied");
                _gpsLocation = value;
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Adds an event to the list of events for this buoy
        /// </summary>
        /// <param name="event">The event to add to the list events</param>
        public void AddEvent(Event @event)
        {
            if(@event == null)
                throw new ArgumentException("Event must not be null");
            Events.Add(@event);
        }
        #endregion
    }
}