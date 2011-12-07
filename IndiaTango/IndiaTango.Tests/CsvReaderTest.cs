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

            foreach (var sensor in sensors)
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
        public void ReadsCSVWithYYYYMMDDmmhhSeperately()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYY,MM,DD,hh,mm,Test Sensor\r\n2011,05,10,13,00,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYMMDDTogetherAndmmhhTogether()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYYMMDD,hhmm,Test Sensor\r\n20110510,1300,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYMMDDTogetherAndmmhhTogetherSplitBySlash()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYY/MM/DD,hhmm,Test Sensor\r\n2011/05/10,1300,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYMMDDTogetherAndmmhhTogetherSplitByDash()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYY-MM-DD,hhmm,Test Sensor\r\n2011-05-10,1300,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYMMDDTogetherAndmmhhTogetherSplitBySpace()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYY MM DD,hhmm,Test Sensor\r\n2011 05 10,1300,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYMMDDTogetherAndmmhhTogetherSplitByColon()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYYMMDD,hh:mm,Test Sensor\r\n20110510,13:00,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYSeperateMMDDTogetherAndmmhhTogether()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYY,MMDD,hh:mm,Test Sensor\r\n2011,0510,13:00,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

        [Test]
        public void ReadsCSVWithYYYYMMTogetherDDSeperateAndmmhhTogether()
        {
            var fileName = Path.GetTempFileName() + ".csv";

            File.WriteAllText(fileName, "YYYYMM,DD,hh:mm,Test Sensor\r\n201105,10,13:00,100");

            var reader = new CSVReader(fileName);
            var sensors = reader.ReadSensors();

            Assert.AreEqual(1, sensors.Count);
            Assert.AreEqual(100, sensors[0].CurrentState.Values[new DateTime(2011, 5, 10, 13, 0, 0)]);
            Assert.AreEqual(1, sensors[0].CurrentState.Values.Count);
        }

    }
}