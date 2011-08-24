
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace IndiaTango.Models
{
    [DataContract]
    public class Contact
    {
        private string _email;

        private Contact() {} // Necessary for serialisation.

        /// <summary>
        /// Creates a new contact
        /// </summary>
        /// <param name="firstName">The first Name of the contact</param>
        /// <param name="lastName">The second Name of the contact</param>
        /// <param name="email">The email address of the contact</param>
        /// <param name="business">The Business the contact belongs to</param>
        /// <param name="phone">The contact phone number of the contact</param>
        public Contact(string firstName, string lastName, string email, string business, string phone)
        {
            if (firstName == null) throw new ArgumentNullException("firstName");
            if (lastName == null) throw new ArgumentNullException("lastName");
            FirstName = firstName;
            LastName = lastName;
            if(email != null)
                Email = email;
            Business = business;
            Phone = phone;
        }

        #region public variables
        /// <summary>
        /// Gets and sets the first name of the contact
        /// </summary>
        [DataMember(Name="FirstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets and sets the second name of the contact
        /// </summary>
        [DataMember(Name="LastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Gets and sets the email address of the contact, also checks the email for validity
        /// </summary>
        [DataMember(Name="Email")]
        public string Email { get { return _email; } set { if (EmailIsValid(value)) _email = value; else throw new ArgumentException("Invalid Email Address", "Email"); } }

        /// <summary>
        /// Gets and sets the business name of the contact
        /// </summary>
        [DataMember(Name="Business")]
        public string Business { get; set; }

        /// <summary>
        /// Gets and sets the phone number of the contact
        /// </summary>
        [DataMember(Name="Phone")]
        public string Phone { get; set; }
        #endregion

        #region private methods
        /// <summary>
        /// Checks to see if the given email address is valid
        /// </summary>
        /// <param name="email">The email to check</param>
        /// <returns>If the email is valid or not</returns>
        private static bool EmailIsValid(string email)
        {
            var portions = email.Split('@');
            if(portions.Length == 2)
            {
                if (portions[0].Length > 0 && portions[1].Length > 0)
                {
                    var domainPortions = portions[1].Split('.');
                    if (domainPortions.Length > 1)
                    {
                        foreach(var domainPart in domainPortions)
                        {
                            if (domainPart.Length == 0)
                                return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            return String.Format("{0} {1} ({2}) {3} {4}", FirstName, LastName, Business, Phone, Email);
        }
        #endregion
    }
}
