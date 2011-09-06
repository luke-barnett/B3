using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class ContactTest
    {
        private Contact _contact;
        private Contact _anotherContact;

        [SetUp]
        public void SetUp()
        {
            _contact = new Contact("Steve", "Hamilton", "steve@hamilton.co.nz", "Steve Limited", "(07)7533 2343");
            _anotherContact = new Contact("Mike", "Rodgers", "mike@rodgers.co.nz", "Rodgers Emporium", "(07)7553 2343");
        }

        [Test]
        public void GetFirstNameTest()
        {
            Assert.AreEqual("Steve", _contact.FirstName);
            Assert.AreEqual("Mike", _anotherContact.FirstName);
        }

        [Test]
        public void GetLastNameTest()
        {
            Assert.AreEqual("Hamilton", _contact.LastName);
            Assert.AreEqual("Rodgers", _anotherContact.LastName);
        }

        [Test]
        public void GetEmailTest()
        {
            Assert.AreEqual("steve@hamilton.co.nz",_contact.Email);
            Assert.AreEqual("mike@rodgers.co.nz", _anotherContact.Email);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidEmailAddressNoDomain()
        {
            _contact = new Contact("Steve", "Hamilton", "noavalidemail", "Steve Limited", "(07)7533 2343");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidEmailAddressNoDomainWithAtSymbol()
        {
            _contact = new Contact("Steve", "Hamilton", "noavalidemail@", "Steve Limited", "(07)7533 2343");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidEmailAddressNoDomainWithAtSymbolDomainTooShort()
        {
            _contact = new Contact("Steve", "Hamilton", "noavalidemail@foo", "Steve Limited", "(07)7533 2343");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidEmailAddressNoDomainWithAtSymbolDomainJustADot()
        {
            _contact = new Contact("Steve", "Hamilton", "noavalidemail@.", "Steve Limited", "(07)7533 2343");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidEmailAddressNoDomainWithAtSymbolTopDomainIsNothing()
        {
            _contact = new Contact("Steve", "Hamilton", "noavalidemail@foo.", "Steve Limited", "(07)7533 2343");
        }

        [Test]
        public void GetBusiness()
        {
            Assert.AreEqual("Steve Limited", _contact.Business);
            Assert.AreEqual("Rodgers Emporium", _anotherContact.Business);
        }

        [Test]
        public void GetPhoneNumber()
        {
            Assert.AreEqual("(07)7533 2343", _contact.Phone);
            Assert.AreEqual("(07)7553 2343", _anotherContact.Phone);
        }

        [Test]
        public void SetFirstName()
        {
            _contact.FirstName = "Jim";
            Assert.AreEqual("Jim", _contact.FirstName);
        }

        [Test]
        public void SetLastName()
        {
            _contact.LastName = "Hikes";
            Assert.AreEqual("Hikes", _contact.LastName);
        }

        [Test]
        public void SetValidEmail()
        {
            _contact.Email = "newemail@foo.com";
            Assert.AreEqual("newemail@foo.com", _contact.Email);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void SetInvalidEmail()
        {
            _contact.Email = "@com.com";
        }

        [Test]
        public void SetBusiness()
        {
            _contact.Business = "New Business Name";
            Assert.AreEqual("New Business Name", _contact.Business);
        }

        [Test]
        public void SetPhone()
        {
            _contact.Phone = "111";
            Assert.AreEqual("111", _contact.Phone);
        }

        [Test]
        public void TestToString()
        {
            Assert.AreEqual("Steve Hamilton (Steve Limited) (07)7533 2343 steve@hamilton.co.nz", _contact.ToString());
            Assert.AreEqual("Mike Rodgers (Rodgers Emporium) (07)7553 2343 mike@rodgers.co.nz", _anotherContact.ToString());
        }

        [Test]
        public void NullEmail()
        {
            _contact = new Contact("Steve", "Hamilton", null, null, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullFirstName()
        {
            _contact = new Contact(null,"Hamilton", null, null, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullLastName()
        {
            _contact = new Contact("Steve", null, null, null, null);
        }

        [Test]
        public void EqualityTest()
        {
            var A = new Contact("Steve", "Hamilton", "steve@hamilton.co.nz", "Steve Limited", "(07)7533 2343");
            var B = new Contact("Steve", "Hamilton", "steve@hamilton.co.nz", "Steve Limited", "(07)7533 2343");

            Assert.AreEqual(A, B);
        }

        [Test]
        public void GetSetNextIDValid()
        {
            var A = new Contact("Steve", "Hamilton", "steve@hamilton.co.nz", "Steve Limited", "(07)7533 2343");
            Contact.NextID = 1;

            Assert.AreEqual(Contact.NextID, 1);
            Assert.AreEqual(Contact.NextID, 2);

            Contact.NextID = 10;
            Assert.AreEqual(Contact.NextID, 10);
            Assert.AreEqual(Contact.NextID, 11);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetInvalidZeroNextID()
        {
            Contact.NextID = 0;
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetInvalidNegativeNextID()
        {
            Contact.NextID = -1;
        }
    }
}
