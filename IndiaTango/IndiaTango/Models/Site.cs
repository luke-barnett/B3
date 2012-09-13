using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ProtoBuf;

namespace IndiaTango.Models
{
    [Serializable]
    [ProtoContract]
    public class Site
    {
        private int _iD;
        private string _name;
        private string _owner;
        private Contact _primaryContact;
        private Contact _secondaryContact;
        private GPSCoords _gpsLocation;
        private List<NamedBitmap> _images;

        /// <summary>
        /// The location to export to on save
        /// </summary>
        public static string ExportPath
        {
            get { return Common.AppDataPath + "\\ExportedSites.xml"; }
        }

        private Site()
        {
            Events = new List<Event>();
        } // Necessary for serialisation.

        /// <summary>
        /// Creates a new Site.
        /// </summary>
        /// <param name="iD">The unique ID field of the Site</param>
        /// <param name="name">The name of this site</param>
        /// <param name="owner">The name of the owner of the Site</param>
        /// <param name="primaryContact">The details of the primary contact</param>
        /// <param name="secondaryContact">The details of the secondary contact</param>
        /// <param name="gpsLocation">The GPS coordinates of the Site</param>
        public Site(int iD, string name, string owner, Contact primaryContact, Contact secondaryContact, GPSCoords gpsLocation)
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
            _gpsLocation = gpsLocation;
            Events = new List<Event>();
        }

        #region Public variables
        /// <summary>
        /// Sets and gets the ID of the Site
        /// </summary>
        [ProtoMember(1)]
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
        [ProtoMember(2)]
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
        [ProtoMember(3)]
        public string Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;
            }
        }

        /// <summary>
        /// The sites primary contact id
        /// </summary>
        [ProtoMember(4)]
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

        /// <summary>
        /// The sites secondary contact id
        /// </summary>
        [ProtoMember(5)]
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

        /// <summary>
        /// The images of the site
        /// </summary>
        public List<NamedBitmap> Images
        {
            get { return _images; }
            set { _images = value; }
        }

        /// <summary>
        /// Sets and gets the details of the primary contact for this Site
        /// </summary>
        [ProtoMember(8)]
        public Contact PrimaryContact
        {
            get { return _primaryContact; }
            set
            {
                _primaryContact = value;
            }
        }

        /// <summary>
        /// Sets and gets the details of the secondary contact for this Site
        /// </summary>
        [ProtoMember(9)]
        public Contact SecondaryContact
        {
            get { return _secondaryContact; }
            set { _secondaryContact = value; }
        }

        /// <summary>
        /// Gets the list of events for this Site
        /// </summary>
        [ProtoMember(11)]
        public List<Event> Events { get; private set; }

        /// <summary>
        /// Sets and gets the GPS location of this Site
        /// </summary>
        [ProtoMember(12)]
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

        /// <summary>
        /// Notes to be made about the site
        /// </summary>
        [ProtoMember(14)]
        public string SiteNotes { get; set; }

        /// <summary>
        /// Notes to be made about the site
        /// </summary>
        public string EditingNotes { get { return DataEditingNotes == null ? string.Empty : DataEditingNotes.OrderBy(x => x.Key).Aggregate("", (x, y) => string.Format("\r\n\t{0} - {1}", y.Key, y.Value)); }}

        /// <summary>
        /// The set of data editing notes made for the site
        /// </summary>
        [ProtoMember(15)]
        public Dictionary<DateTime, string> DataEditingNotes { get; set; }

        /// <summary>
        /// The elevation of the site
        /// </summary>
        [ProtoMember(16)]
        public float Elevation { get; set; }

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

        /// <summary>
        /// Adds a note about editing
        /// </summary>
        /// <param name="timestamp">The timestamp of the note</param>
        /// <param name="note">The note</param>
        public void AddEditingNote(DateTime timestamp, string note)
        {
            if(DataEditingNotes == null)
                DataEditingNotes = new Dictionary<DateTime, string>();

            DataEditingNotes[timestamp] = note;
        }

        /// <summary>
        /// Removes a note from the editing notes
        /// </summary>
        /// <param name="timestamp">The note to remove</param>
        public void RemoveEditingNote(DateTime timestamp)
        {
            if(DataEditingNotes == null)
                return;
            var fullTimestamp = DataEditingNotes.First(x => (x.Key - timestamp).TotalMilliseconds < 1000000);

            DataEditingNotes.Remove(fullTimestamp.Key);
        }

        #endregion

        /// <summary>
        /// Gets the next Site ID value
        /// </summary>
        public static int NextID
        {
            get
            {
                var i = 1;
                var currentIds = Directory.GetFiles(Common.DatasetSaveLocation).Select(x => x.Substring(x.LastIndexOf('\\') + 1, x.Length - x.LastIndexOf('\\') - 4)).Select(x => x.Substring(0, x.IndexOf(' '))).ToArray();
                while (true)
                {
                    if (currentIds.Contains(i.ToString("00")))
                        i++;
                    else
                        return i;
                }
            }
        }

        /// <summary>
        /// Checks the equality of two sites
        /// </summary>
        /// <param name="obj">The object to check against</param>
        /// <returns>If they are equal or not</returns>
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

            return (site.GpsLocation.Equals(GpsLocation) && site.Id == Id &&
                    site.Owner == Owner && site.PrimaryContact.Equals(PrimaryContact) &&
                    ctwo && site.Name == Name);
        }

        /// <summary>
        /// Converts the site to it's string representation
        /// </summary>
        /// <returns>The string representation of the site</returns>
        public override string ToString()
        {
            return Name + " (" + GpsLocation.DecimalDegreesLatitude + ", " +
                   GpsLocation.DecimalDegreesLongitude + ")" + ", owned by " + Owner;
        }
    }
}