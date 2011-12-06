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
        private MemoryStream _ms;
        private string _tempFile;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.GetTempFileName();
            _ms = new MemoryStream();
            _baseTime = DateTime.Now;
            _ds = new Dataset(new Site(1, "Super Site", "Jimmy Jones", new Contact("Jimmy", "Jones", "jim@jones.com", "Jones Industries", "5551234"), null, null, new GPSCoords(1, 1)));

            var sensor = new Sensor("Super Sensor", null, _ds);

            sensor.RawData.Values[_baseTime] = 0;
            sensor.RawData.Values[_baseTime.AddHours(1)] = 1;
            sensor.RawData.Values[_baseTime.AddHours(2)] = 2;
            sensor.RawData.Values[_baseTime.AddHours(3)] = 3;
            sensor.RawData.Values[_baseTime.AddHours(4)] = 4;
            sensor.RawData.Values[_baseTime.AddHours(5)] = 5;
            sensor.RawData.Values[_baseTime.AddHours(6)] = 6;
            sensor.RawData.Values[_baseTime.AddHours(7)] = 7;
            sensor.RawData.Values[_baseTime.AddHours(8)] = 8;
            sensor.RawData.Values[_baseTime.AddHours(9)] = 9;
            sensor.RawData.Values[_baseTime.AddHours(10)] = 10;

            _ds.Sensors = new List<Sensor> { sensor };
        }

        [Test]
        public void SerializeToMemoryStream()
        {
            Serializer.Serialize(_ms, _ds);
        }

        [Test]
        public void DeSerializeFromMemoryStream()
        {
            SerializeToMemoryStream();
            _ms.Position = 0;
            var clone = Serializer.Deserialize<Dataset>(_ms);
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
        }

        [Test]
        public void SerializeToFile()
        {
            using (var fileStream = File.Create(_tempFile))
                Serializer.Serialize(fileStream, _ds);
        }

        [Test]
        public void DeSerializeFromFile()
        {
            Dataset clone;
            SerializeToFile();
            using (var fileStream = File.OpenRead(_tempFile))
                clone = Serializer.Deserialize<Dataset>(fileStream);
        }
    }
}
