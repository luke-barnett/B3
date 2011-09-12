using System.IO;
using System.Linq;
using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class CSVReaderTest
    {
        private CSVReader _reader;
        private const string TestDataFileName = "../../Test Data/lakeTutira120120110648.csv";

        [SetUp]
        public void Setup()
        {
            _reader = new CSVReader(TestDataFileName);
        }

        [Test]
        public void ConstructorTest()
        {
            var reader = new CSVReader(TestDataFileName);
        }

        [Test]
        public void ReadSensorInformationTest()
        {
            var sensors = _reader.ReadSensors();
            Assert.NotNull(sensors);
            Assert.IsTrue(sensors.Count > 0);
            Assert.AreEqual(27, sensors.Count);
            Assert.IsTrue(sensors[0].CurrentState.Values.Count != 0);

            var sensorNames = (new string[] { "Temperature1", "Temperature2", "Temperature3", "Temperature4", "Temperature5", "Temperature6", "Temperature7", "Temperature8", "Temperature9", "Temperature10", "BatteryVolts", "DOSurfaceSat", "DODeepSat", "Chlorophyll", "Phycocyanin", "Turbidity", "LightLevel", "DOSurfaceCont", "DODeepCont", "WaterColumnDepth(ABS)", "WindDirection", "WindSpeed", "AirTemperature", "Humidity", "BarometricPressure", "Rainfall", "Hail" }).ToList();

            foreach(var sensor in sensors)
            {
                Assert.IsTrue(sensorNames.Contains(sensor.Name));
                sensorNames.Remove(sensor.Name);
            }

            var hailSensor = sensors.Where(sensor => sensor.Name == "Hail").First();
            Assert.IsNotNull(hailSensor);
            Assert.AreEqual(53873, hailSensor.CurrentState.Values.Count);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void FileNotFound()
        {
            _reader = new CSVReader("NotEvenAFile.csv");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void BadFileName()
        {
            _reader = new CSVReader("CannotTellIfIAmCSV");
        }

        [Test]
        public void ReadsCSVWithIndividualDatesCorrectly()
        {
            var testData = "mm,yyyy,dd,nn,hh,Awesome Sensor\r\n08,04,2011,15,00,100\r\n";

            var path = Path.Combine(Common.TestDataPath, "datasetIndividualColumnsTest.csv");
            File.WriteAllText(path, testData);

            var r = new CSVReader(path);
            var list = r.ReadSensors();

            var sensorValue = list[0].CurrentState.Values;

            Assert.AreEqual(sensorValue[new DateTime(2011, 8, 4, 0, 15, 0)], 100);
            Assert.AreEqual(1, sensorValue.Count);
        }
    }
}