using System;
using System.Diagnostics;
using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    public class MissingValuesDetectorTests
    {
        MissingValuesDetector missingValuesDetector;

        [SetUp]
        public void SetUp()
        {
            missingValuesDetector = new MissingValuesDetector();
        }

        [Test]
        public void TestName()
        {
            Assert.AreEqual("Missing Values", missingValuesDetector.Name);
        }

        [Test]
        [RequiresSTA]
        public void SettingsGrid()
        {
            Assert.NotNull(missingValuesDetector.SettingsGrid);
        }

        [Test]
        public void TestMissingValues()
        {
            var contact = new Contact("Jim", "Does", "jim@email.com", "Lollipops", "837773");
            var dataSet = new Dataset(new Site(4, "New Site", "Tim Jones", contact, contact, new GPSCoords(0, 0)));

            var sensor = new Sensor("Dummy Sensor", "Does stuff", 10, 0, "C", 5, dataSet);

            sensor.AddState(new SensorState(sensor, DateTime.Now));
            sensor.CurrentState.Values.Add(new DateTime(1990, 5, 1, 4, 0, 0), 15);
            sensor.CurrentState.Values.Add(new DateTime(1990, 5, 1, 5, 0, 0), 15);
            sensor.CurrentState.Values.Add(new DateTime(1991, 8, 2, 0, 0, 0), 15);

            dataSet.AddSensor(sensor);

            dataSet.DataInterval = 60;

            dataSet.HighestYearLoaded = 1;

            Assert.AreEqual(60, dataSet.DataInterval);

            var missingValues = missingValuesDetector.GetDetectedValues(sensor);

            for (var i = new DateTime(1990, 5, 1, 6, 0, 0); i < new DateTime(1991, 8, 2, 0, 0, 0); i = i.AddHours(1))
            {
                Assert.Contains(new ErroneousValue(i, missingValuesDetector, sensor), missingValues);
            }
        }
    }
}
