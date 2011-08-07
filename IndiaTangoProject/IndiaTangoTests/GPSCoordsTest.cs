using System.Text;
using System.Collections.Generic;
using System.Linq;
using IndiaTangoProject.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IndiaTangoTests
{
    [TestClass]
    public class GPSCoordsTest
    {
        [TestMethod]
        public void GetGPSCoordsDecimalDegrees()
        {
            GPSCoords degreeTest = new GPSCoords(-37.788047f, 175.310512f);

            Assert.AreEqual(-37.788047f, degreeTest.DecimalDegreesLatitude);
            Assert.AreEqual(175.310512f, degreeTest.DecimalDegreesLongitude);
        }

        [TestMethod]
        public void SetGPSCoordsDecimalDegrees()
        {
            GPSCoords degreeTest = new GPSCoords(0, 0);
            degreeTest.DecimalDegreesLatitude = -37.788047f;

            Assert.AreEqual(-37.788047f, degreeTest.DecimalDegreesLatitude);

            degreeTest.DecimalDegreesLongitude = 175.310512f;

            Assert.AreEqual(175.310512f, degreeTest.DecimalDegreesLongitude);
        }
    }
}
