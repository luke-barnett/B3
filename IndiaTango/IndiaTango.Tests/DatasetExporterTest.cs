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
		private readonly Dataset _data = new Dataset(new Buoy(1, "Your Mother", "Kerry", Contact, Contact, Contact, new GPSCoords(0,0)), DateTime.Now, DateTime.Now.AddDays(2));

		#region Tests

    	[Test]
        public void ExportAsCSVTest()
    	{
    		CSVReader reader = new CSVReader(Path.Combine(_inputFilePath));
    		_data.Sensors = reader.ReadSensors();
			_exporter = new DatasetExporter(_data);
			_exporter.Export(_outputFilePath,ExportFormat.CSV);
			//Assert.IsTrue(CompareFiles(_outputFilePath,_inputFilePath));
        }

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullArguementConstructorTest()
		{
			_exporter = new DatasetExporter(null);
		}

		#endregion

		#region CompareFunctions

		public bool CompareFiles(string filePathOne, string filePathTwo)
		{
			return Tools.GenerateMD5HashFromFile(filePathOne) == Tools.GenerateMD5HashFromFile(filePathTwo);
		}

		#endregion

    }
}
