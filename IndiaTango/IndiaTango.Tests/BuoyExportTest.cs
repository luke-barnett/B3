using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class BuoyExportTest
    {
        [Test]
        public void ExportsOneBuoyCorrectly()
        {
            string singleBuoyXML = "";

            Contact c = new Contact("Bob", "Smith", "bob@smith.com", "Bob's Bakery", "123456");
            Buoy buoy = new Buoy(Buoy.NextID, "Random Site", "An Owner", c, c, c, new GPSCoords(27, -95));

            Assert.AreEqual(singleBuoyXML, buoy.Export());
        }
    }
}
