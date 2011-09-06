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
    	private DatasetExporter _exporter;

		//Secondary contact cannot be null? Argh...
		//Also need a setter on the Dataset class for the sensor list
    	private static readonly Contact Contact = new Contact("K", "A", "k@a.com", "", "0");
		private readonly Dataset _data = new Dataset(new Buoy(1, "Your Mother", "Kerry", Contact, Contact, Contact, new GPSCoords(0,0)));

		#region Tests

    	[Test]
        public void ExportAsCSVNoEmptyLines()
    	{
    		CSVReader reader = new CSVReader(Path.Combine(_inputFilePath));
    		_data.Sensors = reader.ReadSensors();
			_exporter = new DatasetExporter(_data);
			_exporter.Export(_outputFilePath,ExportFormat.CSV,false,false);
			Assert.AreEqual(Tools.GenerateMD5HashFromFile(_outputFilePath), Tools.GenerateMD5HashFromFile(_inputFilePath));
        }

        [Test]
        public void ExportAsCSVEmptyLinesIncluded()
        {
            CSVReader reader = new CSVReader(Path.Combine(_inputFilePath));
            _data.Sensors = reader.ReadSensors();
            _exporter = new DatasetExporter(_data);
            _exporter.Export(_outputFilePath, ExportFormat.CSV, true,false);
            Assert.AreEqual(File.ReadLines(_outputFilePath).Count(), _data.DataPointCount + 1);
        }

        [Test]
        public void ExportAsCSVNoEmptyLinesAndMetaData()
        {
            CSVReader reader = new CSVReader(Path.Combine(_inputFilePath));
            _data.Sensors = reader.ReadSensors();
            _exporter = new DatasetExporter(_data);
            _exporter.Export(_outputFilePath, ExportFormat.CSV, false, true);
            Assert.AreEqual(Tools.GenerateMD5HashFromFile(_outputFilePath), Tools.GenerateMD5HashFromFile(_inputFilePath));
        }

        [Test]
        public void ExportAsCSVCorrectValueCount()
        {
            var dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            var givenDataSet = new Dataset(new Buoy(1, "Steven", "Kerry", Contact, Contact, Contact, new GPSCoords(0, 0)));

            // 24 * 4 = # of 15 min slots in a day
            Assert.AreEqual(givenDataSet.DataPointCount, (24 * 4) * 2 + 1);

            dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            givenDataSet = new Dataset(new Buoy(1, "Steven", "Kerry", Contact, Contact, Contact, new GPSCoords(0, 0)));

            // 24 * 4 = # of 15 min slots in a day
            Assert.AreEqual(givenDataSet.DataPointCount, (24 * 4) + 1);
        }

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullArguementConstructorTest()
		{
			_exporter = new DatasetExporter(null);
		}

		#endregion

    }
}
