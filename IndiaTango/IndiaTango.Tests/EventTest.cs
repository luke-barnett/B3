using System;
using System.Collections.Generic;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class EventTest
    {
        private Event _e;
        private DateTime _timeStamp;
        private string _action;
        [SetUp]
        public void SetUp()
        {
            _timeStamp = new DateTime(2011, 8, 10, 13, 13, 0);
            _action = "Site put in water";
            _e = new Event(_timeStamp,_action);
        }

        [Test]
        public void TimeStampgetSetTest()
        {
            Assert.AreEqual(_timeStamp,_e.TimeStamp);
            _timeStamp = new DateTime(2011,1,1,0,0,0);
            _e.TimeStamp = _timeStamp;
            Assert.AreEqual(_timeStamp,_e.TimeStamp);
        }

        [Test]
        public void ActionGetSetTest()
        {
            Assert.AreEqual("Site put in water",_e.Action);
            _e.Action = "What";
            Assert.AreEqual("What",_e.Action);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ActionNullConstuctorTest()
        {
            _e = new Event(new DateTime(654, 2, 23, 8, 2, 0), null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ActionEmptyConstructorTest()
        {
             _e = new Event(new DateTime(654, 2, 23, 8, 2, 0), "");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ActionNullPropertyTest()
        {
            _e.Action = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ActionEmptyPropertyTest()
        {
            _e.Action = "";
        }

        [Test]
        public void EqualityTest()
        {
            var A = new Event(new DateTime(2011, 7, 4, 12, 59, 0), "Created a Site");
            var B = new Event(new DateTime(2011, 7, 4, 12, 59, 0), "Created a Site");

            Assert.AreEqual(A, B);
        }

        [Test]
        public void ListEmptyEqualityTest()
        {
            Assert.AreEqual(new List<Event>(), new List<Event>());
        }
    }
}
