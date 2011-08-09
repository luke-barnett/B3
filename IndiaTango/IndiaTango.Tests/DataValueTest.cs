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
            _dataTest = new DataValue(50f, new DateTime(500000));
        }

        [Test]
        public void GetValue()
        {
            Assert.AreEqual(50f, _dataTest.Value);
            var anotherValue = new DataValue(60f, new DateTime(500000));
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
            Assert.AreEqual(new DateTime(500000), _dataTest.TimeStamp);
            var anotherDataValue = new DataValue(40f, new DateTime(1700000));
            Assert.AreEqual(new DateTime(1700000), anotherDataValue.TimeStamp);
        }

        [Test]
        public void SetTimeStamp()
        {
            _dataTest.TimeStamp = new DateTime(1800000);
            Assert.AreEqual(new DateTime(1800000), _dataTest.TimeStamp);
        }
    }
}
