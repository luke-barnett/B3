using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class DataStringReaderTest
    {
        private IDataReader reader;

        [SetUp]
        public void SetUp()
        {
            reader = new DataStringReader("Site:NewBuoy,Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        #region Constructor Tests

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorWithoutACommaToSeperate()
        {
            reader = new DataStringReader("Site:NewBuoySensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorTooManySections()
        {
            reader = new DataStringReader("Site:NewBuoy,Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2],Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorCantDetermineTypeNoColon()
        {
            reader = new DataStringReader("BuoyNewBuoy,Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorCantDetermineTypeTooManyParts()
        {
            reader = new DataStringReader("Site:NewBuoy:MoreInformation,Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorCantDetermineTypeNotRecognised()
        {
            reader = new DataStringReader("Buy:NewBuoy,Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorDuplicateBuoys()
        {
            reader = new DataStringReader("Site:NewBuoy,Site:NewBuoy");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ConstructorDuplicateSensors()
        {
            reader = new DataStringReader("Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2],Sensors:Temperature[0.4;0.8;1.5]&OtherValues[0.0;0.0;1.2]");
        }

        #endregion

        [Test]
        public void ReadSensorInformation()
        {
            //Read in the sensors
            var sensors = reader.ReadSensors();

            //Check the sensors against the dummy data
            Assert.NotNull(sensors);
            Assert.IsTrue(sensors.Count == 2);

            foreach (var sensor in sensors)
            {
                Assert.NotNull(sensor);
            }

            //TODO: Test the data more comprehensively
        }
    }
}
