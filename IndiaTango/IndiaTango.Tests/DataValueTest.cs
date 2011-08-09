using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class DataValueTest
    {
        private DataValue _dataTest;

        [SetUp]
        public void SetUp()
        {
            _dataTest = new DataValue(new DateTime(500000), 50f);
        }

        [Test]
        public void GetValue()
        {
            Assert.AreEqual(50f, _dataTest.Value);
            var anotherValue = new DataValue(new DateTime(500000), 60f);
            Assert.AreEqual(60f, anotherValue.Value);
        }

        [Test]
        public void SetValue()
        {
            _dataTest.Value = 30f;
            Assert.AreEqual(30f, _dataTest.Value);
        }

        [Test]
        public void GetTimeStamp()
        {
            Assert.AreEqual(new DateTime(500000), _dataTest.Timestamp);
            var anotherDataValue = new DataValue(new DateTime(1700000), 40f);
            Assert.AreEqual(new DateTime(1700000), anotherDataValue.Timestamp);
        }

        [Test]
        public void SetTimeStamp()
        {
            _dataTest.Timestamp = new DateTime(1800000);
            Assert.AreEqual(new DateTime(1800000), _dataTest.Timestamp);
        }

        [Test]
        public void Equality()
        {
            Assert.AreEqual(new DataValue(new DateTime(5000000), 30f), new DataValue(new DateTime(5000000), 30f));
            Assert.AreEqual(new DataValue(new DateTime(500600000), 40f), new DataValue(new DateTime(500600000), 40f));
            Assert.AreNotEqual(new DataValue(new DateTime(5000000), 30f), new DataValue(new DateTime(500600000), 40f));
        }
    }
}
