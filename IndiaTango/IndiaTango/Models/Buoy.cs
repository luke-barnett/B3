using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace IndiaTango.Models
{
    [DataContract]
    public class Buoy
    {
        private int _iD;
        private string _site;
        private string _owner;
        private Contact _primaryContact;
        private Contact _secondaryContact;
        private GPSCoords _gpsLocation;

        public static string ExportPath
        {
            get { return Common.AppDataPath + "\\ExportedBuoys.xml"; }
        }

        private Buoy() {} // Necessary for serialisation.

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
                throw new ArgumentException("ID number must a non-negative integer");
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
        [DataMember(Name="ID")]
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
        [DataMember(Name="Site")]
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
        [DataMember(Name="Owner")]
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
        [DataMember(Name="PrimaryContact")]
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
        [DataMember(Name="SecondaryContact")]
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
        [DataMember(Name="UniversityContact")]
        public Contact UniversityContact { get; set; }
        
        /// <summary>
        /// Gets the list of events for this buoy
        /// </summary>
        [DataMember(Name="Events")]
        public List<Event> Events { get; private set; }

        /// <summary>
        /// Sets and gets the GPS location of this buoy
        /// </summary>
        [DataMember(Name="GPSLocation")]
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

        private static int _nextID = 0;

        public static int NextID
        {
            get { return ++_nextID;  }
            set
            {
                if ((value - 1) < -1)
                    throw new ArgumentException("Next ID value must be greater than or equal to 0.");

                _nextID = value - 1;
            }
        }

        public static void ExportAll(List<Buoy> buoys)
        {
            NetDataContractSerializer dcs = new NetDataContractSerializer();
            var stream = new FileStream(ExportPath, FileMode.Create);
            dcs.Serialize(stream, buoys);
            stream.Close();
        }

        public static List<Buoy> ImportAll()
        {
            NetDataContractSerializer dcs = new NetDataContractSerializer();
            var stream = new FileStream(ExportPath, FileMode.Open);
            var list = (List<Buoy>)dcs.Deserialize(stream);
            stream.Close();
            return list;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Buoy))
                return false;

            var buoy = obj as Buoy;

            for (int i = 0; i < buoy.Events.Count; i++)
                if (!buoy.Events[i].Equals(Events[i]))
                    return false;

            var gps = buoy.GpsLocation.Equals(GpsLocation);
            var id = buoy.Id == Id;
            var owner = buoy.Owner == Owner;
            var cone = buoy.PrimaryContact.Equals(PrimaryContact);
            var ctwo = buoy.SecondaryContact.Equals(SecondaryContact);
            var cthree = buoy.UniversityContact.Equals(UniversityContact);
            var site = buoy.Site == Site;

            return (buoy.GpsLocation.Equals(GpsLocation) && buoy.Id == Id &&
                    buoy.Owner == Owner && buoy.PrimaryContact.Equals(PrimaryContact) &&
                    buoy.SecondaryContact.Equals(SecondaryContact) && buoy.Site == Site &&
                    buoy.UniversityContact.Equals(UniversityContact));
        }

        // TODO: test event num mismatch
    }
}