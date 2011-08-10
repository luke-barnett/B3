using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorTest
    {
        private Sensor _sensor1;
        private Sensor _sensor2;
        private Sensor _sensor3;

        [SetUp]
        public void SetUp()
        {
            _sensor1 = new Sensor("Temperature","Temperature at 10m",100,20,"°C",0.003f,"Awesome Industries");
            _sensor2 = new Sensor("DO","Dissolved Oxygen in the water", 50,0, "%",5.6f,"SensorPlus");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidNameTest()
        {
            _sensor3 = new Sensor("", "", 0, 0, "%",0,"");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidUnitTest()
        {
            _sensor3 = new Sensor("Temperature", "", 0, 0, "",0,"");
        }

        [Test]
        public void GetNameTest()
        {
            Assert.AreEqual("Temperature", _sensor1.Name);
            Assert.AreEqual("DO", _sensor2.Name);
        }

        [Test]
        public void GetDescriptionTest()
        {
            Assert.AreEqual("Temperature at 10m",_sensor1.Description);
            Assert.AreEqual("Dissolved Oxygen in the water", _sensor2.Description);
        }

        [Test]
        public void GetUpperLimitTest()
        {
            Assert.AreEqual(100,_sensor1.UpperLimit);
            Assert.AreEqual(50, _sensor2.UpperLimit);
        }

        [Test]
        public void GetLowerLimitTest()
        {
            Assert.AreEqual(20,_sensor1.LowerLimit);
            Assert.AreEqual(0, _sensor2.LowerLimit);
        }

        [Test]
        public void GetUnitTest()
        {
            Assert.AreEqual("°C",_sensor1.Unit);
            Assert.AreEqual("%", _sensor2.Unit);
        }

        [Test]
        public void GetMaxRateOfChangeTest()
        {
            Assert.AreEqual(0.003f,_sensor1.MaxRateOfChange);
            Assert.AreEqual(5.6f, _sensor2.MaxRateOfChange);
        }
       
        [Test]
        public void GetManufacturerTest()
        {
            Assert.AreEqual("Awesome Industries", _sensor1.Manufacturer);
        }

        [Test]
        public void SetNameTest()
        {
            _sensor1.Name = "Humidity";
            Assert.AreEqual("Humidity",_sensor1.Name);

            _sensor1.Name = "Rainfall";
            Assert.AreEqual("Rainfall", _sensor1.Name);
        }

        [Test]
        public void SetUpperLimitTest()
        {
            _sensor1.UpperLimit = 120;
            Assert.AreEqual(120,_sensor1.UpperLimit);

            _sensor2.UpperLimit = 45;
            Assert.AreEqual(45, _sensor2.UpperLimit);
        }

        [Test]
        public void SetLowerLimitTest()
        {
            _sensor1.LowerLimit = 0;
            Assert.AreEqual(0, _sensor1.LowerLimit);

            _sensor2.LowerLimit = 5;
            Assert.AreEqual(5, _sensor2.LowerLimit);
        }

        [Test]
        public void SetLowerUnitTest()
        {
            _sensor1.Unit = "°F";
            Assert.AreEqual("°F", _sensor1.Unit);

            _sensor2.Unit = "$";
            Assert.AreEqual("$", _sensor2.Unit);
        }

        [Test]
        public void SetMaxRateOfChangeTest()
        {
            _sensor1.MaxRateOfChange = 0.03f;
            Assert.AreEqual(0.03f, _sensor1.MaxRateOfChange);

            _sensor2.MaxRateOfChange = 6f;
            Assert.AreEqual(6f, _sensor2.MaxRateOfChange);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void InvalidSetNameTest()
        {
            _sensor1.Name = "";
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void InvalidSetUnitTest()
        {
            _sensor1.Unit = "";
        }
    }
}
