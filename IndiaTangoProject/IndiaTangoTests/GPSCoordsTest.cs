﻿using System;
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
        private const double PRECISION = 0.001;

        [TestMethod]
        public void GetGPSCoordsDecimalDegrees()
        {
            GPSCoords degreeTest = new GPSCoords(-37.788047M, 175.310512M);

            Assert.AreEqual(-37.788047M, degreeTest.DecimalDegreesLatitude);
            Assert.AreEqual(175.310512M, degreeTest.DecimalDegreesLongitude);
        }

        [TestMethod]
        public void SetGPSCoordsDecimalDegrees()
        {
            GPSCoords degreeTest = new GPSCoords(0, 0);
            degreeTest.DecimalDegreesLatitude = -37.788047M;

            Assert.AreEqual(-37.788047M, degreeTest.DecimalDegreesLatitude);

            degreeTest.DecimalDegreesLongitude = 175.310512M;

            Assert.AreEqual(175.310512M, degreeTest.DecimalDegreesLongitude);
        }

        [TestMethod]
        public void GPSCoordinatesDMSToDecimalDegrees()
        {
            GPSCoords dmsTest = new GPSCoords("S37 47 16", "E175 18 37");

            Assert.AreEqual(-37.788047d, Convert.ToDouble(dmsTest.DecimalDegreesLatitude), PRECISION);

            Assert.AreEqual(175.310512d, Convert.ToDouble(dmsTest.DecimalDegreesLongitude), PRECISION);
        }

        [TestMethod]
        public void GetGPSCoordinatesDMS()
        {
            GPSCoords dmsTest = new GPSCoords("S37 47 16", "E175 18 37");

            //Pression difference
            Assert.AreEqual("S37 47 15", dmsTest.DMSLatitude);

            Assert.AreEqual("E175 18 37", dmsTest.DMSLongitude);
        }

        [TestMethod]
        public void SetGPSCoordinatesDMS()
        {
            GPSCoords dmsTest = new GPSCoords("", "");
            dmsTest.DMSLatitude = "S37 47 16";

            Assert.AreEqual("S37 47 15", dmsTest.DMSLatitude); //Pression difference

            dmsTest.DMSLongitude = "E175 18 37";

            Assert.AreEqual("E175 18 37", dmsTest.DMSLongitude);
        }
    }
}
