using System;
using System.Text;
using IndiaTango.Models;
using NUnit.Framework;
using System.IO;
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
		private readonly Dataset _data = new Dataset(new Buoy(1, "Your Mother", "Kerry", Contact, Contact, Contact, new GPSCoords(0,0)), new DateTime(2009,1,9,14,45,0), new DateTime(2011,1,12,6,45,0));

		#region Tests

    	[Test]
        public void ExportAsCSVTest()
    	{
    		CSVReader reader = new CSVReader(Path.Combine(_inputFilePath));
    		_data.Sensors = reader.ReadSensors();
			_exporter = new DatasetExporter(_data);
			_exporter.Export(_outputFilePath,ExportFormat.CSV,false);
			Assert.AreEqual(Tools.GenerateMD5HashFromFile(_outputFilePath), Tools.GenerateMD5HashFromFile(_inputFilePath));
        }

        [Test]
        public void ExportAsCSVCorrectValueCount()
        {
            var dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            var givenDataSet = new Dataset(new Buoy(1, "Steven", "Kerry", Contact, Contact, Contact, new GPSCoords(0, 0)), dateTime, dateTime.AddDays(2));

            // 24 * 4 = # of 15 min slots in a day
            Assert.AreEqual(givenDataSet.DataPointCount, (24 * 4) * 2 + 1);

            dateTime = new DateTime(2011, 8, 4, 0, 0, 0);
            givenDataSet = new Dataset(new Buoy(1, "Steven", "Kerry", Contact, Contact, Contact, new GPSCoords(0, 0)), dateTime, dateTime.AddDays(1));

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
