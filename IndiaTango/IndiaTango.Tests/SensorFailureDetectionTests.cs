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
            var sensorState = new SensorState(DateTime.Now, new Dictionary<DateTime, float>());
            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(1), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 0);
            _temperatureSensor.AddState(sensorState);

            Assert.IsFalse(_temperatureSensor.IsFailing);
        }

        [Test]
        private void DetectNonFailingSensorNoFailedValues()
        {
            var sensorState = new SensorState(DateTime.Now, new Dictionary<DateTime, float>());
            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(1), 25.5f);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 21.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 26.2f);
            sensorState.Values.Add(DateTime.Now.AddDays(4), 20f);
            _temperatureSensor.AddState(sensorState);

            Assert.IsFalse(_temperatureSensor.IsFailing);
        }

        [Test]
        public void DetectNonFailingSensorNonConsecutiveValues()
        {
            var sensorState = new SensorState(DateTime.Now, new Dictionary<DateTime, float>());
            sensorState.Values.Add(DateTime.Now, 0f);
            sensorState.Values.Add(DateTime.Now.AddDays(1), 0f);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 0f);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 26.2f);
            sensorState.Values.Add(DateTime.Now.AddDays(4), 0f);
            _temperatureSensor.AddState(sensorState);

            Assert.IsFalse(_temperatureSensor.IsFailing);
        }

        [Test]
        public void DetectFailingSensorConsecutiveValuesBoundary()
        {
            var sensorState = new SensorState(DateTime.Now, new Dictionary<DateTime, float>());
            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(1), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(4), 0);
            _temperatureSensor.AddState(sensorState);

            Assert.IsTrue(_temperatureSensor.IsFailing);
        }

        [Test]
        public void DetectFailingSensorNonConsecutiveValues()
        {
            var sensorState = new SensorState(DateTime.Now, new Dictionary<DateTime, float>());
            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(1), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 22.5f);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(4), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(5), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(6), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(7), 23.3f);
            _temperatureSensor.AddState(sensorState);

            Assert.IsTrue(_temperatureSensor.IsFailing);
        }
        #endregion
    }
}
