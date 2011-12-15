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
        private CSVReader _reader;
        private Dataset _ds1, _ds2;
        private Site _b = new Site(1, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new GPSCoords(32, 5));
        private DateTime _startTime1, _startTime2, _endTime1,_endTime2 ;
        private List<Sensor> _sensors;
        private const string FifteenMinuteIntervalData = "../../Test Data/15MinuteIntervalData.csv";
        private const string TwentyMinuteIntervalData = "../../Test Data/20MinuteIntervalData.csv";
        private const string FifteenMinuteIntervalMissingData = "../../Test Data/15MinuteIntervalMissingData.csv";

        [SetUp]
        public void Setup()
        {
            _reader = new CSVReader(Path.Combine(Common.TestDataPath, "lakeTutira120120110648_extra_small.csv"));
            _sensors = _reader.ReadSensors();
            _startTime1 = new DateTime(46, 4, 12, 6, 48, 20);
            _endTime1 = new DateTime(84, 2, 8, 2, 4, 20);
            _startTime2 = new DateTime(2009,1,9,14,45,0);
            _endTime2 = new DateTime(2009, 1, 14, 19, 15, 0);
            
            _ds1 = new Dataset(_b);
            _ds2 = new Dataset(_b,_sensors);
        }
        [Test]
        public void BuoyGetSetTest()
        {
            Assert.AreEqual(_b, _ds1.Site);
            _b = new Site(2, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new GPSCoords(32, 5));
            _ds1.Site = _b;
            Assert.AreEqual(_b, _ds1.Site);
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
        public void SetsDataIntervalCorrectly()
        {
            _ds2.DataInterval = 50;
            Assert.AreEqual(50, _ds2.DataInterval);

            _ds1.DataInterval = 100;
            Assert.AreEqual(100, _ds1.DataInterval);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void SetsNegativeDataIntervalCorrectly()
        {
            _ds2.DataInterval = -50;
        }

        [Test]
        public void CorrectExpectedDataPointCount()
        {
            Assert.AreEqual(499, _ds2.ExpectedDataPointCount);    
        }

        [Test]
        public void CorrectActualDataPointCount()
        {
            _reader = new CSVReader(Path.Combine(Common.TestDataPath, "lakeTutira120120110648.csv"));
            Dataset ds = new Dataset(_b, _reader.ReadSensors());
            Assert.AreEqual(53873, ds.ActualDataPointCount);
        }

        [Test]
        public void DetectIntervalWithNoMissingValues()
        {
            _reader = new CSVReader(FifteenMinuteIntervalData);
            Dataset ds = new Dataset(_b,_reader.ReadSensors());
            Assert.AreEqual(15, ds.DataInterval);
        }

        [Test]
        public void DetectIntervalWithMissingValues()
        {
            _reader = new CSVReader(FifteenMinuteIntervalMissingData);
            Dataset ds = new Dataset(_b, _reader.ReadSensors());
            Assert.AreEqual(15, ds.DataInterval);
        }

        [Test]
        public void MovesSensorsCorrectly()
        {
            var A = new Sensor("Temperature at 10m", "A");
            var B = new Sensor("Temperature at 50m", "B");
            var C = new Sensor("Temperature at 100m", "C");
            var D = new Sensor("Temperature at 200m", "D");
            var E = new Sensor("Temperature at 500m", "E");

            _ds1.Sensors.AddRange(new Sensor[] { A, B, C, D, E });

            _ds1.MoveSensorTo(A, C);

            Assert.AreEqual(0, _ds1.Sensors.IndexOf(B));
            Assert.AreEqual(1, _ds1.Sensors.IndexOf(C));
            Assert.AreEqual(2, _ds1.Sensors.IndexOf(A));
            Assert.AreEqual(3, _ds1.Sensors.IndexOf(D));
            Assert.AreEqual(4, _ds1.Sensors.IndexOf(E));

            _ds1.MoveSensorTo(E, C);

            Assert.AreEqual(0, _ds1.Sensors.IndexOf(B));
            Assert.AreEqual(1, _ds1.Sensors.IndexOf(E));
            Assert.AreEqual(2, _ds1.Sensors.IndexOf(C));
            Assert.AreEqual(3, _ds1.Sensors.IndexOf(A));
            Assert.AreEqual(4, _ds1.Sensors.IndexOf(D));
        }
    }
}
