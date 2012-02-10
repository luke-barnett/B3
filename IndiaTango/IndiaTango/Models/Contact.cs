using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// Contact object
    /// </summary>
    [Serializable]
    [DataContract]
    [ProtoContract]
    public class Contact
    {
        private string _email;

        /// <summary>
        /// Gets the file location where exported contacts will be stored
        /// </summary>
        public static string ExportPath
        {
            get { return Path.Combine(Common.AppDataPath, "ExportedContacts.xml"); }
        }

        private Contact() { } // Necessary for serialisation.
        private int _id;

        /// <summary>
        /// The ID of the contact
        /// </summary>
        [DataMember]
        [ProtoMember(1)]
        public int ID
        {
            get { return _id; }
            set
            {
                if (value >= _nextID)
                    _nextID = value + 1;

                _id = value;
            }
        }

        public Contact(string title, string firstName, string lastName, string email, string business, string phone)
            : this(firstName, lastName, email, business, phone, -1)
        {
            Title = title;
        }

        public Contact(string firstName, string lastName, string email, string business, string phone)
            : this(firstName, lastName, email, business, phone, -1) { }

        /// <summary>
        /// Creates a new contact
        /// </summary>
        /// <param name="firstName">The first Name of the contact</param>
        /// <param name="lastName">The second Name of the contact</param>
        /// <param name="email">The email address of the contact</param>
        /// <param name="business">The Business the contact belongs to</param>
        /// <param name="phone">The contact phone number of the contact</param>
        /// <param name="id">Optional identifier for this contact. Specify as -1 to have an auto-incrementing value assigned during construction.</param>
        public Contact(string firstName, string lastName, string email, string business, string phone, int id)
        {
            if (firstName == null) throw new ArgumentNullException("firstName");
            if (lastName == null) throw new ArgumentNullException("lastName");
            FirstName = firstName;
            LastName = lastName;
            if (email != null)
                Email = email;
            Business = business;
            Phone = phone;
            // Give it a fresh ID
            ID = (id > -1) ? id : NextID;
        }

        #region public variables

        /// <summary>
        /// Gets or sets the title of the contact
        /// </summary>
        [DataMember(Name = "Title")]
        [ProtoMember(2)]
        public string Title { get; set; }

        /// <summary>
        /// Gets and sets the first name of the contact
        /// </summary>
        [DataMember(Name = "FirstName")]
        [ProtoMember(3)]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets and sets the second name of the contact
        /// </summary>
        [DataMember(Name = "LastName")]
        [ProtoMember(4)]
        public string LastName { get; set; }

        /// <summary>
        /// Gets and sets the email address of the contact, also checks the email for validity
        /// </summary>
        [DataMember(Name = "Email")]
        [ProtoMember(5)]
        public string Email { get { return _email; } set { if (EmailIsValid(value)) _email = value; else throw new ArgumentException("Invalid Email Address"); } }

        /// <summary>
        /// Gets and sets the business name of the contact
        /// </summary>
        [DataMember(Name = "Business")]
        [ProtoMember(6)]
        public string Business { get; set; }

        /// <summary>
        /// Gets and sets the phone number of the contact
        /// </summary>
        [DataMember(Name = "Phone")]
        [ProtoMember(7)]
        public string Phone { get; set; }



        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is Contact))
                return false;

            var contact = obj as Contact;

            return contact.Business == Business && contact.Email == Email && contact.FirstName == FirstName &&
                   contact.LastName == LastName && contact.Phone == Phone && contact.Title == Title;
        }

        #region private methods
        /// <summary>
        /// Checks to see if the given email address is valid
        /// </summary>
        /// <param name="email">The email to check</param>
        /// <returns>If the email is valid or not</returns>
        private static bool EmailIsValid(string email)
        {
            var portions = email.Split('@');
            if (portions.Length == 2)
            {
                if (portions[0].Length > 0 && portions[1].Length > 0)
                {
                    var domainPortions = portions[1].Split('.');
                    if (domainPortions.Length > 1)
                    {
                        return domainPortions.All(domainPart => domainPart.Length != 0);
                    }
                }
            }
            return false;
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            return Title == null || string.IsNullOrWhiteSpace(Title) ? string.Format("{0} {1} ({2}) {3} {4}", FirstName, LastName, Business, Phone, Email) : string.Format("{0} {1} {2} ({3}) {4} {5}", Title, FirstName, LastName, Business, Phone, Email);
        }
        #endregion

        public static void ExportAll(ObservableCollection<Contact> contacts)
        {
            var serializer = new DataContractSerializer(typeof(ObservableCollection<Contact>));
            var stream = new FileStream(ExportPath, FileMode.Create);
            serializer.WriteObject(stream, contacts);
            stream.Close();
        }

        public static ObservableCollection<Contact> ImportAll()
        {
            var serializer = new DataContractSerializer(typeof(ObservableCollection<Contact>));

            if (!File.Exists(ExportPath))
                return new ObservableCollection<Contact>();

            var stream = new FileStream(ExportPath, FileMode.Open);
            var list = (ObservableCollection<Contact>)serializer.ReadObject(stream);
            stream.Close();

            // TODO: Next ID for when imported!
            return list;
        }

        private static int _nextID = 1;

        public static int NextID
        {
            get { return _nextID++; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("The next ID for a contact must be greater than 0.");

                _nextID = value;
            }
        }
    }
}
