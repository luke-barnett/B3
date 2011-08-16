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
    	private readonly string _outputFilePath = Path.Combine(Common.AppDataPath, "ExampleOutput", "test.csv");
		private readonly string _exampleFilePath = Path.Combine(Common.AppDataPath, "ExampleOutput", "example.csv");
    	private DatasetExporter _exporter;

		//Secondary contact cannot be null? Argh...
    	private static readonly Contact Contact = new Contact("K", "A", "k@a.com", "", "0");
		private readonly Dataset _data = new Dataset(new Buoy(1, "Your Mother", "Kerry", Contact, Contact, Contact, new GPSCoords(0,0)), DateTime.Now, DateTime.Now.AddDays(2));

		#region Tests
    	[Test]
        public void ConstructorTest()
        {
			_exporter = new DatasetExporter(_data);
        }

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullArguementConstructorTest()
		{
			_exporter = new DatasetExporter(null);
		}
		#endregion

		#region CompareFunctions
		public bool CompareHashes(string filePathOne, string filePathTwo)
		{
			return Tools.GenerateMD5HashFromFile(filePathOne) == Tools.GenerateMD5HashFromFile(filePathTwo);
		}

		
		#endregion

		#region RandomCode
		/*
		 * Dataset data = new Dataset(null,DateTime.Now,DateTime.Now);
			DatasetExporter exporter = new DatasetExporter(data);
    		exporter.Export(OutputFilePath,ExportFormat.CSV);
			Assert.IsTrue(CompareHashes(OutputFilePath, ExampleFilePath));
		 * */
		#endregion
    }
}
