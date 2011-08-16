using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorFailureDetectionTests
    {
        private Sensor _temperatureSensor;

        [SetUp]
        public void SetUp()
        {
            _temperatureSensor = new Sensor("Temperature", "C");
        }

        #region Sensor Failure Detection Tests
        [Test]
        public void DetectNonFailingSensorBoundary()
        {
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0) }));
            _temperatureSensor.AddState(sensorState);

            Assert.IsFalse(_temperatureSensor.IsFailing);
        }

        [Test]
        private void DetectNonFailingSensorNoFailedValues()
        {
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 25.5f), new DataValue(DateTime.Now, 21.3f), new DataValue(DateTime.Now, 26.2f), new DataValue(DateTime.Now, 20f) }));
            _temperatureSensor.AddState(sensorState);

            Assert.IsFalse(_temperatureSensor.IsFailing);
        }

        [Test]
        public void DetectNonFailingSensorNonConsecutiveValues()
        {
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 0f), new DataValue(DateTime.Now, 0f), new DataValue(DateTime.Now, 0f), new DataValue(DateTime.Now, 26.2f), new DataValue(DateTime.Now, 0f) }));
            _temperatureSensor.AddState(sensorState);

            Assert.IsFalse(_temperatureSensor.IsFailing);
        }

        [Test]
        public void DetectFailingSensorConsecutiveValuesBoundary()
        {
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0) }));
            _temperatureSensor.AddState(sensorState);

            Assert.IsTrue(_temperatureSensor.IsFailing);
        }

        [Test]
        public void DetectFailingSensorNonConsecutiveValues()
        {
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 22.5f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 23.3f) }));
            _temperatureSensor.AddState(sensorState);

            Assert.IsTrue(_temperatureSensor.IsFailing);
        }
        #endregion
    }
}
