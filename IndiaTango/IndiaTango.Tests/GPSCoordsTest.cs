using System;
using IndiaTango.Models;
using NUnit.Framework;


namespace IndiaTango.Tests
{
    [TestFixture]
    public class GPSCoordsTest
    {
        private const double PRECISION = 0.001;

        [Test]
        public void GetGPSCoordsDecimalDegrees()
        {
            GPSCoords degreeTest = new GPSCoords(-37.788047M, 175.310512M);

            Assert.AreEqual(-37.788047M, degreeTest.DecimalDegreesLatitude);
            Assert.AreEqual(175.310512M, degreeTest.DecimalDegreesLongitude);
        }

        [Test]
        public void SetGPSCoordsDecimalDegrees()
        {
            GPSCoords degreeTest = new GPSCoords(0, 0);
            degreeTest.DecimalDegreesLatitude = -37.788047M;

            Assert.AreEqual(-37.788047M, degreeTest.DecimalDegreesLatitude);

            degreeTest.DecimalDegreesLongitude = 175.310512M;

            Assert.AreEqual(175.310512M, degreeTest.DecimalDegreesLongitude);
        }

        [Test]
        public void GPSCoordinatesDMSToDecimalDegrees()
        {
            GPSCoords dmsTest = new GPSCoords("S37 47 16", "E175 18 37");

            Assert.AreEqual(-37.788047d, Convert.ToDouble(dmsTest.DecimalDegreesLatitude), PRECISION);

            Assert.AreEqual(175.310512d, Convert.ToDouble(dmsTest.DecimalDegreesLongitude), PRECISION);
        }

        [Test]
        public void GetGPSCoordinatesDMS()
        {
            GPSCoords dmsTest = new GPSCoords("S37 47 16", "E175 18 37");

            Assert.AreEqual("S37 47 15", dmsTest.DMSLatitude); //Pression difference

            Assert.AreEqual("E175 18 37", dmsTest.DMSLongitude);
        }

        [Test]
        public void SetGPSCoordinatesDMS()
        {
            GPSCoords dmsTest = new GPSCoords("", "");
            dmsTest.DMSLatitude = "S37 47 16";

            Assert.AreEqual("S37 47 15", dmsTest.DMSLatitude); //Pression difference

            dmsTest.DMSLongitude = "E175 18 37";

            Assert.AreEqual("E175 18 37", dmsTest.DMSLongitude);
        }

        [Test]
        public void GPSCoordinatesDecimalDegreesToDMS()
        {
            GPSCoords degreeTest = new GPSCoords(-37.788047M, 175.310512M);

            Assert.AreEqual("S37 47 16", degreeTest.DMSLatitude);

            Assert.AreEqual("E175 18 37", degreeTest.DMSLongitude);
        }

        [Test]
        public void EqualityTest()
        {
            var A = new GPSCoords(4, 17);
            var B = new GPSCoords(4, 17);

            Assert.AreEqual(A, B);
        }

        // TODO: more break it to make it!
        [Test]
        public void CorrectlyParsesDMS()
        {
            GPSCoords degreeTest = GPSCoords.Parse("S37 47 16", "E175 18 37");

            Assert.AreEqual(-37.788047d, Convert.ToDouble(degreeTest.DecimalDegreesLatitude), PRECISION);
            Assert.AreEqual(175.310512d, Convert.ToDouble(degreeTest.DecimalDegreesLongitude), PRECISION);
        }

        [Test]
        public void CorrectlyParsesDecimalDegrees()
        {
            GPSCoords degreeTest = GPSCoords.Parse("-37.788047", "175.310512");

            Assert.AreEqual("S37 47 16", degreeTest.DMSLatitude);

            Assert.AreEqual("E175 18 37", degreeTest.DMSLongitude);
        }
    }
}
