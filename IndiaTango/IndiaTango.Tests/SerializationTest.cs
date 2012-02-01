using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using IndiaTango.Models;
using ProtoBuf;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SerializationTest
    {
        private Dataset _ds;
        private DateTime _baseTime;
        private string _tempFile;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.GetTempFileName();
            _baseTime = DateTime.Now;
            _ds = new Dataset(new Site(1, "Super Site", "Jimmy Jones", new Contact("Jimmy", "Jones", "jim@jones.com", "Jones Industries", "5551234"), null, new GPSCoords(1, 1)));

            var sensor = new Sensor("Super Sensor", null, _ds);

            sensor.RawData.Values[_baseTime] = 0;
            sensor.RawData.Values[_baseTime.AddHours(1)] = 1;
            sensor.RawData.Values[_baseTime.AddHours(2)] = 2;
            sensor.RawData.Values[_baseTime.AddHours(3)] = 3;
            sensor.RawData.Values[_baseTime.AddHours(4)] = 4;
            sensor.RawData.Values[_baseTime.AddHours(6)] = 6;
            sensor.RawData.Values[_baseTime.AddHours(7)] = 7;
            sensor.RawData.Values[_baseTime.AddHours(8)] = 8;
            sensor.RawData.Values[_baseTime.AddHours(9)] = 9;
            sensor.RawData.Values[_baseTime.AddHours(14)] = 14;
            sensor.RawData.Values[_baseTime.AddHours(15)] = 15;

            _ds.Sensors = new List<Sensor> { sensor };
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_ds.SaveLocation))
                File.Delete(_ds.SaveLocation);

            if(File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        [Test]
        public void SerializeToFileUsingDataSetMethod()
        {
            _ds.SaveToFile();
        }

        [Test]
        public void DeSerializeFromFileUsingDataSetMethod()
        {
            SerializeToFileUsingDataSetMethod();
            var clone = Dataset.LoadDataSet(_ds.SaveLocation);
            TestClone(clone);
        }

        private void TestClone(Dataset clone)
        {
            Assert.AreEqual(_ds.Site, clone.Site);
            Assert.AreEqual(_ds.Sensors,clone.Sensors);
        }
    }
}
