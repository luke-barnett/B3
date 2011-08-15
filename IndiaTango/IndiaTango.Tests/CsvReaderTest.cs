using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class CsvReaderTest
    {
        private CsvReader _reader;

        [SetUp]
        public void Setup()
        {
            _reader = new CsvReader("../../../../../lakeTutira120120110648.csv");
        }

        [Test]
        public void ConstructorTest()
        {
            var reader = new CsvReader("../../../../../lakeTutira120120110648.csv");
        }

        [Test]
        public void ReadSensorInformationTest()
        {
            var sensors = _reader.ReadSensors();
            Assert.NotNull(sensors);
        }

    }
}
