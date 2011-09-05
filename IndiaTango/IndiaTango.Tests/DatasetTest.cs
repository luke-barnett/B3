using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class DatasetTest
    {
        private Dataset _ds1, _ds2;
        private Buoy _b;
        private DateTime _startTime1, _startTime2, _endTime1,_endTime2 ;
        private List<Sensor> _sensors;

        [SetUp]
        public void Setup()
        {
            CSVReader reader = new CSVReader(Path.Combine(Common.TestDataPath, "lakeTutira120120110648_extra_small.csv"));
            _sensors = reader.ReadSensors();
            _startTime1 = new DateTime(46, 4, 12, 6, 48, 20);
            _endTime1 = new DateTime(84, 2, 8, 2, 4, 20);
            _startTime2 = new DateTime(2009,1,9,14,45,0);
            _endTime2 = new DateTime(2009, 1, 14, 19, 15, 0);
            _b = new Buoy(1, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), null, new GPSCoords(32, 5));
            _ds1 = new Dataset(_b, _startTime1, _endTime1);
            _ds2 = new Dataset(_b,_sensors);
        }
        [Test]
        public void BuoyGetSetTest()
        {
            Assert.AreEqual(_b, _ds1.Buoy);
            _b = new Buoy(2, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), null, new GPSCoords(32, 5));
            _ds1.Buoy = _b;
            Assert.AreEqual(_b, _ds1.Buoy);
        }

        [Test]
        public void StartTimeStampGetTest()
        {
            Assert.AreEqual(_startTime1, _ds1.StartTimeStamp);
        }

        [Test]
        public void EndTimeStampGetTest()
        {
            Assert.AreEqual(_endTime1, _ds1.EndTimeStamp);
        }

        [Test]
        public void DynamicStartTimeStampGetTest()
        {
            Assert.AreEqual(_startTime2, _ds2.StartTimeStamp);
        }

        [Test]
        public void DynamicEndTimeStampGetTest()
        {
            Assert.AreEqual(_endTime2, _ds2.EndTimeStamp);
        }


        [Test]
        public void SensorListTest()
        {
            var s1 = new Sensor("temp", "deg");
            var s2 = new Sensor("DO", "%");
            var sensors = new List<Sensor> { s1, s2 };
            _ds1.AddSensor(s1);
            _ds1.AddSensor(s2);
            Assert.AreEqual(sensors, _ds1.Sensors);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddNullSensorTest()
        {
            _ds1.AddSensor(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EndTimeBeforeStartTimeTest()
        {
            new Dataset(_b, _endTime1, _startTime1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EndTimeSameAsStartTest()
        {
            new Dataset(_b, _startTime1, _startTime1);
        }
    }
}
