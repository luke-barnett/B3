using System;
using System.Text;
using IndiaTango.Models;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace IndiaTango.Tests
{
    [TestFixture]
    public class DatasetExporterTest
    {
        private readonly string _outputFilePath = Path.Combine(Common.TestDataPath, "dataSetExporterTest.csv");
        private readonly string _inputFilePath = Path.Combine(Common.TestDataPath, "lakeTutira120120110648.csv");
        public static string DatasetOutputWithIndividualColumns = "DD,MM,YYYY,hh,mm,Awesome Sensor\r\n04,08,2011,00,15,100\r\n04,08,2011,00,30,100\r\n04,08,2011,00,45,100\r\n04,08,2011,01,00,100\r\n";

        //Secondary contact cannot be null? Argh...
        //Also need a setter on the Dataset class for the sensor list
        private static readonly Contact Contact = new Contact("K", "A", "k@a.com", "", "0");
        private readonly Dataset _data = new Dataset(new Site(1, "Your Mother", "Kerry", Contact, Contact, new GPSCoords(0, 0)));

        #region Tests

        [Test]
        public void ExportAsCSVNoEmptyLines()
        {
            var reader = new CSVReader(Path.Combine(_inputFilePath));
            _data.Sensors = reader.ReadSensors();
            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, false, false, false, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn);
            Assert.AreEqual(Tools.GenerateMD5HashFromFile(_outputFilePath), Tools.GenerateMD5HashFromFile(_inputFilePath));
        }

        [Test]
        public void ExportAsCSVEmptyLinesIncluded()
        {
            var reader = new CSVReader(Path.Combine(_inputFilePath));
            _data.Sensors = reader.ReadSensors();
            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, true, false, false, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn);
            Assert.AreEqual(File.ReadLines(_outputFilePath).Count(), _data.ExpectedDataPointCount + 1);
        }

        [Test]
        public void ExportAsCSVEmptyLinesIncludedWithAverageEveryHour()
        {
            var reader = new CSVReader(Path.Combine(_inputFilePath));
            _data.Sensors = reader.ReadSensors();
            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, true, false, false, ExportedPoints.HourlyPoints, DateColumnFormat.TwoDateColumn);
            Assert.AreEqual(File.ReadLines(_outputFilePath).Count(), ((_data.ExpectedDataPointCount) / 4) + 1);
        }

        [Test]
        public void ExportAsCSVNoEmptyLinesAndMetaData()
        {
            var reader = new CSVReader(Path.Combine(_inputFilePath));
            _data.Sensors = reader.ReadSensors();
            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, false, true, false, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn);
            Assert.AreEqual(Tools.GenerateMD5HashFromFile(_outputFilePath), Tools.GenerateMD5HashFromFile(_inputFilePath));
        }

        [Test]
        public void ExportAsCSVCorrectValueCount()
        {
            var dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            var givenDataSet = new Dataset(new Site(1, "Steven", "Kerry", Contact, Contact, new GPSCoords(0, 0)));
            var sampleData = new Dictionary<DateTime, float> { { dateTime.AddMinutes(15), 100 }, { dateTime.AddMinutes(30), 100 }, { dateTime.AddMinutes(45), 100 }, { dateTime.AddMinutes(60), 100 } };
            var s = new Sensor("Awesome Sensor", "Awesome");
            var ss = new SensorState(s, DateTime.Now, sampleData, null);
            s.AddState(ss);
            givenDataSet.AddSensor(s);

            Assert.AreEqual(4, givenDataSet.ExpectedDataPointCount);

            dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            givenDataSet = new Dataset(new Site(1, "Steven", "Kerry", Contact, Contact, new GPSCoords(0, 0)));

            sampleData = new Dictionary<DateTime, float> { { dateTime.AddMinutes(60), 100 }, { dateTime.AddMinutes(75), 100 }, { dateTime.AddMinutes(90), 100 }, { dateTime.AddMinutes(105), 100 } };
            s = new Sensor("Awesome Sensor", "Awesome");
            ss = new SensorState(s, DateTime.Now, sampleData, null);
            s.AddState(ss);
            givenDataSet.AddSensor(s);

            Assert.AreEqual(4, givenDataSet.ExpectedDataPointCount);
        }

        [Test]
        public void ExportCSVWithIndividualDateColumns()
        {
            var dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            var givenDataSet = new Dataset(new Site(1, "Steven", "Kerry", Contact, Contact, new GPSCoords(0, 0)));
            var sampleData = new Dictionary<DateTime, float> { { dateTime.AddMinutes(15), 100 }, { dateTime.AddMinutes(30), 100 }, { dateTime.AddMinutes(45), 100 }, { dateTime.AddMinutes(60), 100 } };
            var s = new Sensor("Awesome Sensor", "Awesome");
            var ss = new SensorState(s, DateTime.Now, sampleData, null);
            s.AddState(ss);
            givenDataSet.AddSensor(s);

            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, true, false, false, ExportedPoints.AllPoints, DateColumnFormat.SplitDateColumn);

            Assert.AreEqual(DatasetOutputWithIndividualColumns, File.ReadAllText(_outputFilePath));

        }
        #endregion

        [Test]
        public void ExportsRawDataWhenRequested()
        {
            var reader = new CSVReader(_inputFilePath);
            _data.Sensors = reader.ReadSensors();

            Assert.AreNotEqual(0, _data.Sensors[0].CurrentState.Values[new DateTime(2009, 1, 10, 7, 45, 0)]);

            // Make some changes to check the raw comes out
            var newState = _data.Sensors[0].CurrentState.Clone();
            newState.Values[new DateTime(2009, 1, 10, 7, 45, 0)] = 0;
            _data.Sensors[0].AddState(newState);

            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, true, false, false, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn, true);

            reader = new CSVReader(_outputFilePath + " Raw.csv");
            _data.Sensors = reader.ReadSensors();

            Assert.AreNotEqual(0, _data.Sensors[0].CurrentState.Values[new DateTime(2009, 1, 10, 7, 45, 0)]);
        }

        [Test]
        public void LogReferencesExportTest()
        {
            var reader = new CSVReader(_inputFilePath);
            _data.Sensors = reader.ReadSensors();
            _data.Sensors = new List<Sensor> { _data.Sensors[0] };
            using (var writer = File.CreateText(_outputFilePath + "ChangesTest.csv"))
            {
                writer.WriteLine("Change matrix for file: " + Path.GetFileName(_outputFilePath));
                writer.WriteLine("Date,Time,Temperature");
                for (var time = _data.StartTimeStamp; time <= _data.EndTimeStamp; time = time.AddMinutes(_data.DataInterval))
                {
                    writer.WriteLine(time.ToString("dd/MM/yyyy,HH:mm") + ",1");
                }
            }
            var ll = new LinkedList<int>();
            ll.AddFirst(1);

            for (var time = _data.StartTimeStamp; time <= _data.EndTimeStamp; time = time.AddMinutes(_data.DataInterval))
            {
                _data.Sensors[0].CurrentState.Changes.Add(time, ll);
            }

            DatasetExporter.Export(_data, _outputFilePath, ExportFormat.CSV, true, false, true);
            Assert.AreEqual(Tools.GenerateMD5HashFromFile(_outputFilePath + "ChangesTest.csv"), Tools.GenerateMD5HashFromFile(_outputFilePath + " Changes Matrix.csv"));
        }
    }
}
