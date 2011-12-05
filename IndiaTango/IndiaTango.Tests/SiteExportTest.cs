using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    /*[TestFixture]
    class SiteExportTest
    {
        #region Test XML
        private string singleSiteXML = "<ArrayOfSite xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Site><Events/><GPSLocation><Latitude>27</Latitude><Longitude>-95</Longitude></GPSLocation><ID>1</ID><Images i:nil=\"true\"/><Name>Random Site</Name><Owner>An Owner</Owner><PrimaryContactID>1</PrimaryContactID><SecondaryContactID>1</SecondaryContactID><UniversityContactID>1</UniversityContactID></Site></ArrayOfSite>";
        private string twoSiteXML = "<ArrayOfSite xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Site><Events/><GPSLocation><Latitude>27</Latitude><Longitude>-95</Longitude></GPSLocation><ID>1</ID><Images i:nil=\"true\"/><Name>Random Site</Name><Owner>An Owner</Owner><PrimaryContactID>1</PrimaryContactID><SecondaryContactID>1</SecondaryContactID><UniversityContactID>1</UniversityContactID></Site><Site><Events/><GPSLocation><Latitude>54</Latitude><Longitude>-12</Longitude></GPSLocation><ID>2</ID><Images i:nil=\"true\"/><Name>Random Site Two</Name><Owner>An Owner</Owner><PrimaryContactID>1</PrimaryContactID><SecondaryContactID>1</SecondaryContactID><UniversityContactID>1</UniversityContactID></Site></ArrayOfSite>";
        #endregion

        private Contact c;
        private Site site;
        private Site _siteTwo;

        [SetUp]
        public void Setup()
        {
            //Site.NextID = 1;
            Contact.NextID = 1;

            c = new Contact("Bob", "Smith", "bob@smith.com", "Bob's Bakery", "123456");
            site = new Site(Site.NextID, "Random Site", "An Owner", c, c, c, new GPSCoords(27, -95));
            _siteTwo = new Site(Site.NextID, "Random Site Two", "An Owner", c, c, c, new GPSCoords(54, -12));

            var contacts = new ObservableCollection<Contact>();
            contacts.Add(c);
            Contact.ExportAll(contacts); // Necessary for testing now!
        }

        [Test]
        public void ExportsOneBuoyCorrectly()
        {
            var buoys = new ObservableCollection<Site>(new Site[] { site });

            //Site.ExportAll(buoys);

            Assert.AreEqual(singleSiteXML, File.ReadAllText(Site.ExportPath));
        }

        [Test]
        public void ExportsTwoBuoysCorrectly()
        {
            var buoys = new ObservableCollection<Site>(new Site[] { site, _siteTwo });

            //Site.ExportAll(buoys);

            Assert.AreEqual(twoSiteXML, File.ReadAllText(Site.ExportPath));
        }

        [Test]
        public void ImportsOneBuoyCorrectly()
        {
            File.WriteAllText(Site.ExportPath, singleSiteXML);

            var buoys = new List<Site>(new Site[] { site });

            var import = Site.ImportAll();

            Assert.AreEqual(buoys, import);
        }

        [Test]
        public void ImportsTwoBuoysCorrectly()
        {
            File.WriteAllText(Site.ExportPath, twoSiteXML);

            var buoys = new List<Site>(new Site[] { site, _siteTwo });

            var result = Site.ImportAll();

            Assert.AreEqual(buoys[0], result[0]); // Necessary because of list equality checks
            Assert.AreEqual(buoys[1], result[1]);
        }

        [Test]
        public void ImportThenExportThenImportWorksCorrectly()
        {
            File.WriteAllText(Site.ExportPath, singleSiteXML);
            var result = Site.ImportAll();

            result.Add(_siteTwo);

            //Site.ExportAll(result);

            var final = Site.ImportAll();

            Assert.AreEqual(final[0], result[0]);
            Assert.AreEqual(final[1], result[1]);
        }

        [Test]
        public void NextIDImportTest()
        {
            File.WriteAllText(Site.ExportPath, twoSiteXML);
            var result = Site.ImportAll();

            Assert.AreEqual(Site.NextID, 3);
        }

        [Test]
        public void ExportFileDoesntExist()
        {
            File.Delete(Site.ExportPath);

            Assert.IsTrue(Site.ImportAll().Count == 0);
        }

        // TODO: test site export when images are involved
        // On that note, what if a saved site is reloaded (not using session loading) - do the images come back?
    }*/
}
