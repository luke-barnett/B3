using System;
using System.Collections.Generic;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class BuoyTest
    {
        private Buoy _testBuoy;
        private Contact _pc;
        private Contact _sc;
        private Contact _uc;
        private GPSCoords _gps;

        [SetUp]
        public void SetUp()
        {
            _pc = new Contact("Kerry", "Arts", "something@gmail.com", "Waikato Uni", "0800 stuff");
            _sc = new Contact("Steven", "McTainsh", "hello@gmail.com", "CMS", "0800 WAIKATO");
            _uc = new Contact("Angela","Martin","angela@gmail.com","Waikato Uni","83745");
            _gps = new GPSCoords(37.5135426m,6.21684m);
            _testBuoy = new Buoy(1,"Lake Rotorua","Chris McBride",_pc,_sc,_uc,_gps);
        }

        [Test]
        public void IdGetSetTest()
        {
            Assert.AreEqual(1, _testBuoy.Id);
            _testBuoy.Id = 2;
            Assert.AreEqual(2, _testBuoy.Id);

        }

        [Test]
        public void SiteGetSetTest()
        {
            Assert.AreEqual("Lake Rotorua",_testBuoy.Site);
            _testBuoy.Site = "Lake Taupo";
            Assert.AreEqual("Lake Taupo", _testBuoy.Site);
        }

        [Test]
        public void OwnerGetSetTest()
        {
            Assert.AreEqual("Chris McBride",_testBuoy.Owner);
            _testBuoy.Owner = "David Hamilton";
            Assert.AreEqual("David Hamilton",_testBuoy.Owner);
        }

        [Test]
        public void PrimaryContactGetSetTest()
        {
            Assert.AreEqual(_pc,_testBuoy.PrimaryContact);
            _pc = new Contact("Luke", "Barnett", "blabla@gmail.com", "Stuff", "02712345");
            _testBuoy.PrimaryContact = _pc;
            Assert.AreEqual(_pc, _testBuoy.PrimaryContact);
        }

        [Test]
        public void SecondayContactGetSetTest()
        {
            Assert.AreEqual(_sc,_testBuoy.SecondaryContact);
            _sc = new Contact("Michael","Baumberger","msb26@waikato.ac.nz","Waikato Aero Club","1235678");
            _testBuoy.SecondaryContact = _sc;
            Assert.AreEqual(_sc, _testBuoy.SecondaryContact);
        }

        [Test]
        public void UniContactGetSetTest()
        {
            Assert.AreEqual(_uc,_testBuoy.UniversityContact);
            _uc = new Contact("Tim","Smith","asdufh@sdfuh.com","DSfgiuh","329874");
            _testBuoy.UniversityContact = _uc;
            Assert.AreEqual(_uc,_testBuoy.UniversityContact);
        }

        [Test]
        public void EventsListTest()
        {
            var testEvent1 = new Event(new DateTime(654, 5, 20, 20, 54, 0), "Buoy made");
            var testEvent2 = new Event(new DateTime(6547, 9, 4, 19, 45, 0), "akdsuhf");
            _testBuoy.AddEvent(testEvent1);
            _testBuoy.AddEvent(testEvent2);
            var testEventList = new List<Event> {testEvent1,testEvent2};
            Assert.AreEqual(testEventList,_testBuoy.Events);
        }

        [Test]
        public void GpsCoordinateGetSetTest()
        {
            Assert.AreEqual(_gps,_testBuoy.GpsLocation);
            _gps = new GPSCoords("N52 31 4","E57 12 45");
            _testBuoy.GpsLocation = _gps;
            Assert.AreEqual(_gps,_testBuoy.GpsLocation);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void IdNegativeTest()
        {
            new Buoy(-1, "sadf", "dsafa", _pc, _sc, _uc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptySiteParamTest()
        {
            new Buoy(1, "", "asdf", _pc, _sc, _uc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullSiteParamTest()
        {
            new Buoy(1, null, "asdf", _pc, _sc, _uc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptyOwnerParamTest()
        {
            new Buoy(1, "asdf", "", _pc, _sc, _uc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullOwnerParamTest()
        {
            new Buoy(1, "asdf", null, _pc, _sc, _uc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullPrimaryContactParamTest()
        {
            new Buoy(1, "asdf", "asdf", null, _sc, _uc, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullSecondaryContactParamTest()
        {
            new Buoy(1, "asdf", "asdf", _pc, null, _uc, _gps);
        }

        [Test]
        public void NullUniContactParamAllowedTest()
        {
            new Buoy(1, "asdf", "asdf", _pc, _sc, null, _gps);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullGpsParamTest()
        {
            new Buoy(1, "asdf", "asdf", _pc, _sc, _uc, null);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NegativeIdPropertyTest()
        {
            _testBuoy.Id = -1;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullSitePropertyTest()
        {
            _testBuoy.Site = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void EmptySitePropertyTest()
        {
            _testBuoy.Site = "";
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullOwnerPropertyTest()
        {
            _testBuoy.Owner = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void EmptyOwnerPropertyTest()
        {
            _testBuoy.Owner = "";
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullPrimaryContactPropertyTest()
        {
            _testBuoy.PrimaryContact = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullSecondaryContactPropertyTest()
        {
            _testBuoy.SecondaryContact = null;
        }

        [Test]
        public void NullUniContactPropertyAllowedTest()
        {
            _testBuoy.UniversityContact = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void NullGpsLocationPropertyTest()
        {
            _testBuoy.GpsLocation = null;
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NullEventAddPropertyTest()
        {
            _testBuoy.AddEvent(null);
        }

        [Test]
        public void AutoIncrementingBuoyIDCorrect()
        {
            Assert.AreEqual(1, Buoy.NextID);
            Assert.AreEqual(2, Buoy.NextID);
        }

        [Test]
        public void ResetBuoyIDCorrect()
        {
            Buoy.NextID = 1;
            Assert.AreEqual(1, Buoy.NextID);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ResetBuoyIDInvalid()
        {
            Buoy.NextID = -1;
        }
    }
}