using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class BuoyExportTest
    {
        #region Test XML
        private string singleBuoyXML = "<ArrayOfBuoy z:Id=\"1\" z:Type=\"System.Collections.Generic.List`1[[IndiaTango.Models.Buoy, IndiaTango, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null]]\" z:Assembly=\"0\" xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><_items z:Id=\"2\" z:Size=\"1\"><Buoy z:Id=\"3\"><Events z:Id=\"4\"><_items z:Id=\"5\" z:Size=\"0\"/><_size>0</_size><_version>0</_version></Events><GPSLocation z:Id=\"6\"><Latitude>27</Latitude><Longitude>-95</Longitude></GPSLocation><ID>1</ID><Owner z:Id=\"7\">An Owner</Owner><PrimaryContact z:Id=\"8\"><Business z:Id=\"9\">Bob's Bakery</Business><Email z:Id=\"10\">bob@smith.com</Email><FirstName z:Id=\"11\">Bob</FirstName><LastName z:Id=\"12\">Smith</LastName><Phone z:Id=\"13\">123456</Phone></PrimaryContact><SecondaryContact z:Ref=\"8\" i:nil=\"true\"/><Site z:Id=\"14\">Random Site</Site><UniversityContact z:Ref=\"8\" i:nil=\"true\"/></Buoy></_items><_size>1</_size><_version>0</_version></ArrayOfBuoy>";
        private string twoBuoyXML = "<ArrayOfBuoy z:Id=\"1\" z:Type=\"System.Collections.Generic.List`1[[IndiaTango.Models.Buoy, IndiaTango, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null]]\" z:Assembly=\"0\" xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><_items z:Id=\"2\" z:Size=\"2\"><Buoy z:Id=\"3\"><Events z:Id=\"4\"><_items z:Id=\"5\" z:Size=\"0\"/><_size>0</_size><_version>0</_version></Events><GPSLocation z:Id=\"6\"><Latitude>27</Latitude><Longitude>-95</Longitude></GPSLocation><ID>1</ID><Owner z:Id=\"7\">An Owner</Owner><PrimaryContact z:Id=\"8\"><Business z:Id=\"9\">Bob's Bakery</Business><Email z:Id=\"10\">bob@smith.com</Email><FirstName z:Id=\"11\">Bob</FirstName><LastName z:Id=\"12\">Smith</LastName><Phone z:Id=\"13\">123456</Phone></PrimaryContact><SecondaryContact z:Ref=\"8\" i:nil=\"true\"/><Site z:Id=\"14\">Random Site</Site><UniversityContact z:Ref=\"8\" i:nil=\"true\"/></Buoy><Buoy z:Id=\"15\"><Events z:Id=\"16\"><_items z:Ref=\"5\" i:nil=\"true\"/><_size>0</_size><_version>0</_version></Events><GPSLocation z:Id=\"17\"><Latitude>54</Latitude><Longitude>-12</Longitude></GPSLocation><ID>2</ID><Owner z:Ref=\"7\" i:nil=\"true\"/><PrimaryContact z:Ref=\"8\" i:nil=\"true\"/><SecondaryContact z:Ref=\"8\" i:nil=\"true\"/><Site z:Id=\"18\">Random Site Two</Site><UniversityContact z:Ref=\"8\" i:nil=\"true\"/></Buoy></_items><_size>2</_size><_version>0</_version></ArrayOfBuoy>";
        #endregion

        private Contact c;
        private Buoy buoy;
        private Buoy buoyTwo;

        [SetUp]
        public void Setup()
        {
            Buoy.NextID = 1;

            c = new Contact("Bob", "Smith", "bob@smith.com", "Bob's Bakery", "123456");
            buoy = new Buoy(Buoy.NextID, "Random Site", "An Owner", c, c, c, new GPSCoords(27, -95));
            buoyTwo = new Buoy(Buoy.NextID, "Random Site Two", "An Owner", c, c, c, new GPSCoords(54, -12));
        }

        [Test]
        public void ExportsOneBuoyCorrectly()
        {
            var buoys = new List<Buoy>(new Buoy[] {buoy});

            Buoy.ExportAll(buoys);

            Assert.AreEqual(singleBuoyXML, File.ReadAllText(Buoy.ExportPath));
        }

        [Test]
        public void ExportsTwoBuoysCorrectly()
        {
            var buoys = new List<Buoy>(new Buoy[] { buoy, buoyTwo });

            Buoy.ExportAll(buoys);

            Assert.AreEqual(twoBuoyXML, File.ReadAllText(Buoy.ExportPath));
        }

        [Test]
        public void ImportsOneBuoyCorrectly()
        {
            File.WriteAllText(Buoy.ExportPath, singleBuoyXML);

            var buoys = new List<Buoy>(new Buoy[] { buoy });

            Assert.AreEqual(buoys, Buoy.ImportAll());
        }

        [Test]
        public void ImportsTwoBuoysCorrectly()
        {
            File.WriteAllText(Buoy.ExportPath, twoBuoyXML);

            var buoys = new List<Buoy>(new Buoy[] { buoy, buoyTwo });

            var result = Buoy.ImportAll();

            Assert.AreEqual(buoys[0], result[0]); // Necessary because of list equality checks
            Assert.AreEqual(buoys[1], result[1]);
        }

        [Test]
        public void ImportThenExportThenImportWorksCorrectly()
        {
            File.WriteAllText(Buoy.ExportPath, singleBuoyXML);
            var result = Buoy.ImportAll();

            result.Add(buoyTwo);

            Buoy.ExportAll(result);

            var final = Buoy.ImportAll();

            Assert.AreEqual(final[0], result[0]);
            Assert.AreEqual(final[1], result[1]);
        }
    }
}
