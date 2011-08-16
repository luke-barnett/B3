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
        #region Sensor Failure Detection Tests
        [Test]
        public void DetectNonFailingSensorBoundary()
        {
            var sensorCuspOfFailing = new Sensor("Temperature", "C");
            var sensorStateCuspOfFailing = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0) }));
            sensorCuspOfFailing.AddState(sensorStateCuspOfFailing);

            Assert.IsFalse(sensorCuspOfFailing.IsFailing);
        }

        [Test]
        private void DetectNonFailingSensorNoFailedValues()
        {
            var sensorNotFailing = new Sensor("Temperature", "C");
            var sensorStateNotFailing = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 25.5f), new DataValue(DateTime.Now, 21.3f), new DataValue(DateTime.Now, 26.2f), new DataValue(DateTime.Now, 20f) }));
            sensorNotFailing.AddState(sensorStateNotFailing);

            Assert.IsFalse(sensorNotFailing.IsFailing);
        }

        [Test]
        public void DetectNonFailingSensorNonConsecutiveValues()
        {
            var sensorNotFailing = new Sensor("Temperature", "C");
            var sensorStateNotFailing = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 0f), new DataValue(DateTime.Now, 0f), new DataValue(DateTime.Now, 0f), new DataValue(DateTime.Now, 26.2f), new DataValue(DateTime.Now, 0f) }));
            sensorNotFailing.AddState(sensorStateNotFailing);

            Assert.IsFalse(sensorNotFailing.IsFailing);
        }

        [Test]
        public void DetectFailingSensorConsecutiveValuesBoundary()
        {
            var sensor = new Sensor("Temperature", "C");
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0) }));
            sensor.AddState(sensorState);

            Assert.IsTrue(sensor.IsFailing);
        }

        [Test]
        public void DetectFailingSensorNonConsecutiveValues()
        {
            var sensor = new Sensor("Temperature", "C");
            var sensorState = new SensorState(DateTime.Now, new List<DataValue>(new DataValue[] { new DataValue(DateTime.Now, 22.3f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 22.5f), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 0), new DataValue(DateTime.Now, 23.3f) }));
            sensor.AddState(sensorState);

            Assert.IsTrue(sensor.IsFailing);
        }
        #endregion
    }
}
