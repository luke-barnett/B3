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
        private Contact _contact;
        private Dataset _dataset;
        private Sensor _sensor1;
        private Site _site;

        [SetUp]
        public void SetUp()
        {
            _sensor1 = new Sensor("Temperature", "C");
            _temperatureSensor = new Sensor("Temperature", "C");
            _temperatureSensor.ErrorThreshold = 4;
            _contact = new Contact("Bob", "Smith", "bob@smith.com", "Bob's Bakery", "1232222");
            _site = new Site(50, "Lake Rotorua", "Bob Smith", _contact, _contact, new GPSCoords(50, 50));
            _dataset =
                new Dataset(_site, new List<Sensor> { { _sensor1 } });
        }

        #region Sensor Failure Detection Tests
        [Test]
        public void DetectNonFailingSensorBoundary()
        {
            var sensorState = new SensorState(_temperatureSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);

            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 5);

            _temperatureSensor.AddState(sensorState);

            var ds = new Dataset(_site, new List<Sensor> { _temperatureSensor });

            Assert.IsFalse(_temperatureSensor.IsFailing(ds));
        }

        [Test]
        public void DetectNonFailingSensorNoFailedValues()
        {
            var sensorState = new SensorState(_temperatureSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);

            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(1), 0);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 2);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 4);
            sensorState.Values.Add(DateTime.Now.AddDays(4), 20f);

            _temperatureSensor.AddState(sensorState);

            var ds = new Dataset(_site, new List<Sensor> { _temperatureSensor });

            Assert.IsFalse(_temperatureSensor.IsFailing(ds));
        }

        [Test]
        public void DetectNonFailingSensorNonConsecutiveValues()
        {
            var sensorState = new SensorState(_temperatureSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);

            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(3), 4);

            _temperatureSensor.AddState(sensorState);

            var ds = new Dataset(_site, new List<Sensor> { _temperatureSensor });

            Assert.IsFalse(_temperatureSensor.IsFailing(ds));
        }

        [Test]
        public void DetectFailingSensorConsecutiveValuesBoundary()
        {
            var sensorState = new SensorState(_temperatureSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);

            sensorState.Values.Add(DateTime.Now, 22.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(5), 4);
            sensorState.Values.Add(DateTime.Now.AddDays(6), 4);
            sensorState.Values.Add(DateTime.Now.AddDays(7), 4);

            _temperatureSensor.AddState(sensorState);

            var ds = new Dataset(_site, new List<Sensor> { _temperatureSensor });
            ds.DataInterval = 60 * 24;

            Assert.IsTrue(_temperatureSensor.IsFailing(ds));
        }

        [Test]
        public void DetectFailingSensorNonConsecutiveValues()
        {
            var sensorState = new SensorState(_temperatureSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            sensorState.Values.Add(DateTime.Now.AddDays(2), 22.5f);
            sensorState.Values.Add(DateTime.Now.AddDays(7), 23.3f);
            sensorState.Values.Add(DateTime.Now.AddDays(8), 4);
            sensorState.Values.Add(DateTime.Now.AddDays(9), 4);

            _temperatureSensor.AddState(sensorState);

            var ds = new Dataset(_site, new List<Sensor> { _temperatureSensor });

            Assert.IsTrue(_temperatureSensor.IsFailing(ds));
        }

        [Test]
        public void SensorStateWithNoValues()
        {
            var sensorState = new SensorState(_temperatureSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);

            _temperatureSensor.AddState(sensorState);

            var ds = new Dataset(_site, new List<Sensor> { _temperatureSensor });

            Assert.IsFalse(_temperatureSensor.IsFailing(ds));
        }
        #endregion
    }
}
