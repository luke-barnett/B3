using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;

namespace IndiaTango.Models
{
    [Serializable]
    [DataContract]
    public class Site
    {
        private int _iD;
        private string _name;
        private string _owner;
        private Contact _primaryContact;
        private Contact _secondaryContact;
        private Contact _universityContact;
        private GPSCoords _gpsLocation;
        private List<NamedBitmap> _images;

        public static string ExportPath
        {
            get { return Common.AppDataPath + "\\ExportedSites.xml"; }
        }

        private Site() { } // Necessary for serialisation.

        /// <summary>
        /// Creates a new Site.
        /// </summary>
        /// <param name="iD">The unique ID field of the Site</param>
        /// <param name="name">The name of this site</param>
        /// <param name="owner">The name of the owner of the Site</param>
        /// <param name="primaryContact">The details of the primary contact</param>
        /// <param name="secondaryContact">The details of the secondary contact</param>
        /// <param name="universityContact">The details of the optional university contact</param>
        /// <param name="gpsLocation">The GPS coordinates of the Site</param>
        public Site(int iD, string name, string owner, Contact primaryContact, Contact secondaryContact, Contact universityContact, GPSCoords gpsLocation)
        {
            if (iD < 0)
                throw new ArgumentException("ID number must a non-negative integer");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Site must not be empty");
            /*if(String.IsNullOrEmpty(owner))
                throw new ArgumentException("Owner must not be empty");
            if(primaryContact == null)
                throw new ArgumentException("Primary contact must not be null");
            if(gpsLocation == null)
                throw new ArgumentException("GPS Location must be supplied");*/

            _iD = iD;
            _name = name;
            _owner = owner;
            _primaryContact = primaryContact;
            _secondaryContact = secondaryContact;
            _universityContact = universityContact;
            _gpsLocation = gpsLocation;
            Events = new List<Event>();
        }

        #region Public variables
        /// <summary>
        /// Sets and gets the ID of the Site
        /// </summary>
        [DataMember(Name = "ID")]
        public int Id
        {
            get { return _iD; }
            set
            {
                if (value < 0)
                    throw new FormatException("ID number must be greater than 1");
                _iD = value;
            }
        }

        /// <summary>
        /// Sets and gets the site name of the Site
        /// </summary>
        [DataMember(Name = "Name")]
        public string Name
        {
            get { return _name; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new FormatException("Site must not be empty");
                _name = value;
            }
        }

        /// <summary>
        /// Sets and gets the owner of the Site
        /// </summary>
        [DataMember(Name = "Owner")]
        public string Owner
        {
            get { return _owner; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new FormatException("Owner must not be empty");
                _owner = value;
            }
        }

        [DataMember]
        public int PrimaryContactID
        {
            get
            {
                if (_primaryContact != null)
                    return _primaryContact.ID;

                return 0;
            }
            private set
            {
                if (_primaryContact == null)
                    _primaryContact = new Contact("", "", "user@domain.com", "", "", 0); // This is only used for serialisation generally, so should be suitable

                _primaryContact.ID = value;
            }
        }

        [DataMember]
        public int SecondaryContactID
        {
            get
            {
                if (_secondaryContact != null)
                    return _secondaryContact.ID;

                return 0;
            }
            private set
            {
                if (_secondaryContact == null)
                    _secondaryContact = new Contact("", "", "user@domain.com", "", "", 0); // This is only used for serialisation generally, so should be suitable

                _secondaryContact.ID = value;
            }
        }

        [DataMember]
        public int UniversityContactID
        {
            get
            {
                if (_universityContact != null)
                    return _universityContact.ID;

                return 0;
            }
            private set
            {
                if (_universityContact == null)
                    _universityContact = new Contact("", "", "user@domain.com", "", "", 0); // This is only used for serialisation generally, so should be suitable

                _universityContact.ID = value;
            }
        }

        [DataMember]
        public List<NamedBitmap> Images
        {
            get { return _images; }
            set { _images = value; }
        }

        /// <summary>
        /// Sets and gets the details of the primary contact for this Site
        /// </summary>
        public Contact PrimaryContact
        {
            get { return _primaryContact; }
            set
            {
                if (value == null)
                    throw new FormatException("Primary contact must not be null");
                _primaryContact = value;
            }
        }

        /// <summary>
        /// Sets and gets the details of the secondary contact for this Site
        /// </summary>
        public Contact SecondaryContact
        {
            get { return _secondaryContact; }
            set { _secondaryContact = value; }
        }

        /// <summary>
        /// Sets and gets the details of the university contact for this Site
        /// </summary>
        public Contact UniversityContact
        {
            get { return _universityContact; }
            set { _universityContact = value; }
        }

        /// <summary>
        /// Gets the list of events for this Site
        /// </summary>
        [DataMember(Name = "Events")]
        public List<Event> Events { get; private set; }

        /// <summary>
        /// Sets and gets the GPS location of this Site
        /// </summary>
        [DataMember(Name = "GPSLocation")]
        public GPSCoords GpsLocation
        {
            get { return _gpsLocation; }
            set
            {
                if (value == null)
                    throw new FormatException("GPS Location must be supplied");
                _gpsLocation = value;
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Adds an event to the list of events for this Site
        /// </summary>
        /// <param name="event">The event to add to the list events</param>
        public void AddEvent(Event @event)
        {
            if (@event == null)
                throw new ArgumentException("Event must not be null");
            Events.Add(@event);
        }
        #endregion

        public static int NextID
        {
            get
            {
                var i = 1;
                var currentIds = Directory.GetFiles(Common.DatasetSaveLocation).Select(x => x.Substring(x.LastIndexOf('\\') + 1, x.Length - x.LastIndexOf('\\') - 4)).Select(x => x.Substring(0, x.IndexOf(' '))).ToArray();
                while (true)
                {
                    if (currentIds.Contains(i.ToString()))
                        i++;
                    else
                        return i;
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Site))
                return false;

            var site = obj as Site;
            bool ctwo, cthree;

            if (site.Events.Count != Events.Count)
                return false; // Must have compatible length lists

            for (int i = 0; i < site.Events.Count; i++)
                if (!site.Events[i].Equals(Events[i]))
                    return false;

            if (site.SecondaryContact != null)
                ctwo = site.SecondaryContact.Equals(SecondaryContact);
            else
                ctwo = SecondaryContact == null;

            if (site.UniversityContact != null)
                cthree = site.UniversityContact.Equals(UniversityContact);
            else
                cthree = UniversityContact == null;

            return (site.GpsLocation.Equals(GpsLocation) && site.Id == Id &&
                    site.Owner == Owner && site.PrimaryContact.Equals(PrimaryContact) &&
                    ctwo && site.Name == Name && cthree);
        }

        public override string ToString()
        {
            return Name + " (" + GpsLocation.DecimalDegreesLatitude + ", " +
                   GpsLocation.DecimalDegreesLongitude + ")" + ", owned by " + Owner;
        }
    }
}