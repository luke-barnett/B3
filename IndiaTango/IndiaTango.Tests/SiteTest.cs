using System;
using System.Collections.Generic;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SiteTest
    {
        private Site _testSite;
        private Contact _pc;
        private Contact _sc;
        private GPSCoords _gps;
        private Site A;
        private Site B;

        [SetUp]
        public void SetUp()
        {
            _pc = new Contact("Kerry", "Arts", "something@gmail.com", "Waikato Uni", "0800 stuff");
            _sc = new Contact("Steven", "McTainsh", "hello@gmail.com", "CMS", "0800 WAIKATO");
            _gps = new GPSCoords(37.5135426m,6.21684m);
            _testSite = new Site(1,"Lake Rotorua","Chris McBride",_pc,_sc,_gps);

            A = new Site(6, "A Site", "David Hamilton",
                             new Contact("David", "Hamilton", "david@hamilton.com", "UoW", "1234567"),
                             new Contact("Stan", "Smith", "stan@smith.com", "CIA", "1212127"),
                             new GPSCoords(49, -2));
            B = new Site(6, "A Site", "David Hamilton",
                             new Contact("David", "Hamilton", "david@hamilton.com", "UoW", "1234567"),
                             new Contact("Stan", "Smith", "stan@smith.com", "CIA", "1212127"),
                             new GPSCoords(49, -2));
        }

        [Test]
        public void IdGetSetTest()
        {
            Assert.AreEqual(1, _testSite.Id);
            _testSite.Id = 2;
            Assert.AreEqual(2, _testSite.Id);

        }

        [Test]
        public void SiteGetSetTest()
        {
            Assert.AreEqual("Lake Rotorua", _testSite.Name);
            _testSite.Name = "Lake Taupo";
            Assert.AreEqual("Lake Taupo", _testSite.Name);
        }

        [Test]
        public void OwnerGetSetTest()
        {
            Assert.AreEqual("Chris McBride",_testSite.Owner);
            _testSite.Owner = "David Hamilton";
            Assert.AreEqual("David Hamilton",_testSite.Owner);
        }

        [Test]
        public void PrimaryContactGetSetTest()
        {
            Assert.AreEqual(_pc,_testSite.PrimaryContact);
            _pc = new Contact("Luke", "Barnett", "blabla@gmail.com", "Stuff", "02712345");
            _testSite.PrimaryContact = _pc;
            Assert.AreEqual(_pc, _testSite.PrimaryContact);
        }

        [Test]
        public void SecondayContactGetSetTest()
        {
            Assert.AreEqual(_sc,_testSite.SecondaryContact);
            _sc = new Contact("Michael","Baumberger","msb26@waikato.ac.nz","Waikato Aero Club","1235678");
            _testSite.SecondaryContact = _sc;
            Assert.AreEqual(_sc, _testSite.SecondaryContact);
        }

        [Test]
        public void EventsListTest()
        {
            var testEvent1 = new Event(new DateTime(654, 5, 20, 20, 54, 0), "Site made");
            var testEvent2 = new Event(new DateTime(6547, 9, 4, 19, 45, 0), "akdsuhf");
            _testSite.AddEvent(testEvent1);
            _testSite.AddEvent(testEvent2);
            var testEventList = new List<Event> {testEvent1,testEvent2};
            Assert.AreEqual(testEventList,_testSite.Events);
        }

        [Test]
        public void GpsCoordinateGetSetTest()
        {
            Assert.AreEqual(_gps,_testSite.GpsLocation);
            _gps = new GPSCoords("N52 31 4","E57 12 45");
            _testSite.GpsLocation = _gps;
            Assert.AreEqual(_gps,_testSite.GpsLocation);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void IdNegativeTest()
        {
            new Site(-1, "sadf", "dsafa", _pc, _sc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptySiteParamTest()
        {
            new Site(1, "", "asdf", _pc, _sc, _gps);
        }

        /*[Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullSiteParamTest()
        {
            new Site(1, null, "asdf", _pc, _sc, _uc, _gps);
        }*/

        /*[Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptyOwnerParamTest()
        {
            new Site(1, "asdf", "", _pc, _sc, _uc, _gps);
        }*/

        /*[Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullOwnerParamTest()
        {
            new Site(1, "asdf", null, _pc, _sc, _uc, _gps);
        }*/

        /*[Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullPrimaryContactParamTest()
        {
            new Site(1, "asdf", "asdf", null, _sc, _uc, _gps);
        }*/

        [Test]
        public void NullSecondaryContactParamTest()
        {
            new Site(1, "asdf", "asdf", _pc, null, _gps);
        }

        [Test]
        public void NullUniContactParamAllowedTest()
        {
            new Site(1, "asdf", "asdf", _pc, _sc, _gps);
        }

        /*[Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullGpsParamTest()
        {
            new Site(1, "asdf", "asdf", _pc, _sc, _uc, null);
        }*/

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NegativeIdPropertyTest()
        {
            _testSite.Id = -1;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullSitePropertyTest()
        {
            _testSite.Name = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void EmptySitePropertyTest()
        {
            _testSite.Name = "";
        }

        [Test]
        public void NullSecondaryContactPropertyTest()
        {
            _testSite.SecondaryContact = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullGpsLocationPropertyTest()
        {
            _testSite.GpsLocation = null;
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullEventAddPropertyTest()
        {
            _testSite.AddEvent(null);
        }

        /*[Test]
        public void AutoIncrementingBuoyIDCorrect()
        {
            Site.NextID = 1; // Start with a consistent state

            Assert.AreEqual(1, Site.NextID);
            Assert.AreEqual(2, Site.NextID);
        }

        [Test]
        public void ResetBuoyIDCorrect()
        {
            Site.NextID = 1;
            Assert.AreEqual(1, Site.NextID);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ResetBuoyIDInvalid()
        {
            Site.NextID = -1;
        }*/

        [Test]
        public void EqualityTest()
        {
            Assert.AreEqual(A, B);
        }

        [Test]
        public void EventListEqualityTest()
        {
            Assert.AreEqual(A.Events, B.Events);
        }

        [Test]
        public void CoordsEqualityTest()
        {
            Assert.AreEqual(A.GpsLocation, B.GpsLocation);
        }

        [Test]
        public void PrimaryContactEqualityTest()
        {
            Assert.AreEqual(A.PrimaryContact, B.PrimaryContact);
        }

        [Test]
        public void SecondaryContactEqualityTest()
        {
            Assert.AreEqual(A.SecondaryContact, B.SecondaryContact);
        }

        [Test]
        public void EqualityTestEventMismatch()
        {
            A.Events.Add(new Event(DateTime.Now, "Created the Site"));
            Assert.IsFalse(A.Equals(B));
        }

        [Test]
        public void EqualityTestEventMismatchOtherWayAround()
        {
            B.Events.Add(new Event(DateTime.Now, "Created the Site"));
            Assert.IsFalse(B.Equals(A));
        }

        // No tests for this for Primary contact because it can't be null
        [Test]
        public void SiteNoSecondaryContactReturnsZeroID()
        {
            B.SecondaryContact = null;
            Assert.AreEqual(0, B.SecondaryContactID);

            var C = new Site(Site.NextID, "Awesome Place", "Awesome", _pc, null, new GPSCoords(5, 10));
            Assert.AreEqual(0, C.SecondaryContactID);
        }

        [Test]
        public void ReportsCorrectContactIDs()
        {
            Assert.AreEqual(B.PrimaryContactID, B.PrimaryContact.ID);
            Assert.AreEqual(B.SecondaryContactID, B.SecondaryContact.ID);
        }
    }
}