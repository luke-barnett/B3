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
			"<ArrayOfContact z:Id=\"1\" z:Type=\"System.Collections.ObjectModel.ObservableCollection`1[[IndiaTango.Models.Contact, IndiaTango, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null]]\" z:Assembly=\"WindowsBase, Version=3.0.0.0, Culture=Neutral, PublicKeyToken=31bf3856ad364e35\" xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><items z:Id=\"2\" z:Type=\"System.Collections.Generic.List`1[[IndiaTango.Models.Contact, IndiaTango, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null]]\" z:Assembly=\"0\"><_items z:Id=\"3\" z:Size=\"4\"><Contact z:Id=\"4\"><Business z:Id=\"5\">Lalala</Business><Email z:Id=\"6\">k@a.com</Email><FirstName z:Id=\"7\">Kerry</FirstName><LastName z:Id=\"8\">Arts</LastName><Phone z:Id=\"9\">123456789</Phone></Contact><Contact i:nil=\"true\"/><Contact i:nil=\"true\"/><Contact i:nil=\"true\"/></_items><_size>1</_size><_version>1</_version></items><_monitor z:Id=\"10\" xmlns:a=\"http://schemas.datacontract.org/2004/07/System.Collections.ObjectModel\"><a:_busyCount>0</a:_busyCount></_monitor></ArrayOfContact>";

		private const string TwoContactsXMLString =
			"<ArrayOfContact z:Id=\"1\" z:Type=\"System.Collections.ObjectModel.ObservableCollection`1[[IndiaTango.Models.Contact, IndiaTango, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null]]\" z:Assembly=\"WindowsBase, Version=3.0.0.0, Culture=Neutral, PublicKeyToken=31bf3856ad364e35\" xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><items z:Id=\"2\" z:Type=\"System.Collections.Generic.List`1[[IndiaTango.Models.Contact, IndiaTango, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null]]\" z:Assembly=\"0\"><_items z:Id=\"3\" z:Size=\"4\"><Contact z:Id=\"4\"><Business z:Id=\"5\">Lalala</Business><Email z:Id=\"6\">k@a.com</Email><FirstName z:Id=\"7\">Kerry</FirstName><LastName z:Id=\"8\">Arts</LastName><Phone z:Id=\"9\">123456789</Phone></Contact><Contact z:Id=\"10\"><Business z:Id=\"11\">AwesomeCompany</Business><Email z:Id=\"12\">l@j.com</Email><FirstName z:Id=\"13\">Leroy</FirstName><LastName z:Id=\"14\">Jenkins</LastName><Phone z:Id=\"15\">022 314 1337</Phone></Contact><Contact i:nil=\"true\"/><Contact i:nil=\"true\"/></_items><_size>2</_size><_version>2</_version></items><_monitor z:Id=\"16\" xmlns:a=\"http://schemas.datacontract.org/2004/07/System.Collections.ObjectModel\"><a:_busyCount>0</a:_busyCount></_monitor></ArrayOfContact>";
		
		private Contact _contactOne;
		private Contact _contactTwo;

		[SetUp]
		public void Setup()
		{
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
	}
}
