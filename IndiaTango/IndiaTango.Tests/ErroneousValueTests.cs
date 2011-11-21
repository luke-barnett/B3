using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class ErroneousValueTests
    {
        private ErroneousValue _value;
        private ErroneousValue _valueWithDetector;
        private MissingValuesDetector _detector;
        private DateTime _time;

        [SetUp]
        public void SetUp()
        {
            _time = DateTime.Now;

            _detector = new MissingValuesDetector();

            _value = new ErroneousValue(_time, 15, null);

            _valueWithDetector = new ErroneousValue(_time, 50, _detector, null);
        }

        [Test]
        public void CanGetTime()
        {
            Assert.AreEqual(_time, _value.TimeStamp);
            Assert.AreEqual(_time, _valueWithDetector.TimeStamp);
        }

        [Test]
        public void CanGetValue()
        {
            Assert.AreEqual(15, _value.Value);
            Assert.AreEqual(50, _valueWithDetector.Value);
        }

        [Test]
        public void CanGetDetectors()
        {
            Assert.IsEmpty(_value.Detectors);
            Assert.Contains(_detector, _valueWithDetector.Detectors);
        }

        [Test]
        public void TestToString()
        {
            Assert.AreEqual(string.Format("{0} {1}", _value.TimeStamp, _value.Value), _value.ToString());
            Assert.AreEqual(string.Format("{0} {1} [{2}]", _valueWithDetector.TimeStamp, _valueWithDetector.Value, _valueWithDetector.Detectors[0].Name), _valueWithDetector.ToString());
        }

        [Test]
        public void TestEquality()
        {
            Assert.IsTrue(_value.Equals(_valueWithDetector));
            Assert.IsFalse(_value.Equals(new ErroneousValue(DateTime.Now.AddDays(1), 15, null)));
        }
    }
}
