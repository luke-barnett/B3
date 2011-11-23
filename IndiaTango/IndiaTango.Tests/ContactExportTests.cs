using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
	[TestFixture]
	class ContactExportTests
	{
		private const string SingleContactXMLString =
            "<ArrayOfContact xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Contact><Business>Lalala</Business><Email>k@a.com</Email><FirstName>Kerry</FirstName><ID>1</ID><LastName>Arts</LastName><Phone>123456789</Phone><Title i:nil=\"true\"/></Contact></ArrayOfContact>";

		private const string TwoContactsXMLString =
            "<ArrayOfContact xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Contact><Business>Lalala</Business><Email>k@a.com</Email><FirstName>Kerry</FirstName><ID>1</ID><LastName>Arts</LastName><Phone>123456789</Phone><Title i:nil=\"true\"/></Contact><Contact><Business>AwesomeCompany</Business><Email>l@j.com</Email><FirstName>Leroy</FirstName><ID>2</ID><LastName>Jenkins</LastName><Phone>022 314 1337</Phone><Title i:nil=\"true\"/></Contact></ArrayOfContact>";
		
		private Contact _contactOne;
		private Contact _contactTwo;

		[SetUp]
		public void Setup()
		{
		    Contact.NextID = 1;
			_contactOne = new Contact("Kerry", "Arts", "k@a.com", "Lalala", "123456789");
			_contactTwo = new Contact("Leroy", "Jenkins", "l@j.com", "AwesomeCompany", "022 314 1337");
		}

		[Test]
		public void ExportSingleContactTest()
		{
			Contact.ExportAll(new ObservableCollection<Contact>(new[] { _contactOne }));
			Assert.AreEqual(SingleContactXMLString,File.ReadAllText(Contact.ExportPath));
		}

		[Test]
		public void ExportTwoContactsTest()
		{
			Contact.ExportAll(new ObservableCollection<Contact>(new[] { _contactOne, _contactTwo }));
			Assert.AreEqual(TwoContactsXMLString, File.ReadAllText(Contact.ExportPath));
		}

		[Test]
		public void ImportSingleContactTest()
		{
			File.WriteAllText(Contact.ExportPath, SingleContactXMLString);

			var result = Contact.ImportAll();
			var expected = new List<Contact>(new[] { _contactOne });

			Assert.AreEqual(expected[0], result[0]);
			Assert.AreEqual(result,expected);
		}

		[Test]
		public void ImportTwoContactsTest()
		{
			File.WriteAllText(Contact.ExportPath,TwoContactsXMLString);
			
			var result = Contact.ImportAll();
			var expected = new List<Contact>(new[] {_contactOne, _contactTwo});
			
			Assert.AreEqual(expected[0], result[0]);
			Assert.AreEqual(expected[1], result[1]);
		}

		[Test]
		public void CombinedImportAndExportTest()
		{
			File.WriteAllText(Contact.ExportPath, SingleContactXMLString);
			var firstImport = Contact.ImportAll();

			firstImport.Add(_contactTwo);

			Contact.ExportAll(firstImport);

			var secondImport = Contact.ImportAll();

			Assert.AreEqual(firstImport[0], secondImport[0]);
			Assert.AreEqual(firstImport[1], secondImport[1]);
		}
	}
}
