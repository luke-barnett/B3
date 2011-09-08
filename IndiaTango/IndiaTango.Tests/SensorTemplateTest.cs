using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorTemplateTest
    {
        private const string UNIT = "C";
        private const float UPPER_LIMIT = 500;
        private const float LOWER_LIMIT = 200;
        private const float MAX_CHANGE = 10;

        private const string SensorTemplateSingleXML =
            "<ArrayOfSensorTemplate xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><SensorTemplate><LowerLimit>200</LowerLimit><MatchingStyle>Contains</MatchingStyle><MaximumRateOfChange>10</MaximumRateOfChange><Pattern>Temp</Pattern><Unit>C</Unit><UpperLimit>500</UpperLimit></SensorTemplate></ArrayOfSensorTemplate>";

        private string _pattern = "Temp";

        [Test]
        public void ConstructionWithValidSensor()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, _pattern);
            Assert.Pass();
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructionWithBlankSensorUnit()
        {
            var s = new SensorTemplate("", UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, _pattern);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructionWithNullSensorUnit()
        {
            var s = new SensorTemplate(null, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, _pattern);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructionWithWhitespaceSensorUnit()
        {
            var s = new SensorTemplate("    ", UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, _pattern);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructionWithNullPattern()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, null);
        }

        [Test]
        public void SensorNameMatchedCorrectly()
        {
            var testSensor = new Sensor("Temperature", "C");
            var testSensorTwo = new Sensor("Dissolved Oxygen", "Q");

            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");
            Assert.IsTrue(s.Matches(testSensor));
            Assert.IsFalse(s.Matches(testSensorTwo));

            s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.StartsWith, "Temp");
            Assert.IsTrue(s.Matches(testSensor));
            Assert.IsFalse(s.Matches(testSensorTwo));

            s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.EndsWith, "Temp");
            Assert.IsFalse(s.Matches(testSensor));

            s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.EndsWith, "n");
            Assert.IsTrue(s.Matches(testSensorTwo));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException), ExpectedMessage="Cannot assign template values to a null sensor.")]
        public void ProvideDefaultValuesNullSensor()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");
            s.ProvideDefaultValues(null);
        }

        [Test]
        public void ProvidesDefaultValuesWhenMatches()
        {
            var testSensor = new Sensor("Temperature", "C", 150, 50, "F", 8.3f, "Random Corporation", "ASA932832");
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");

            s.ProvideDefaultValues(testSensor);

            Assert.AreEqual(testSensor.Unit, s.Unit);
            Assert.AreEqual(testSensor.UpperLimit, s.UpperLimit);
            Assert.AreEqual(testSensor.LowerLimit, s.LowerLimit);
            Assert.AreEqual(testSensor.MaxRateOfChange, s.MaximumRateOfChange);
        }
        
        [Test]
        public void GetterTests()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");

            Assert.AreEqual(UNIT, s.Unit);
            Assert.AreEqual(UPPER_LIMIT, s.UpperLimit);
            Assert.AreEqual(LOWER_LIMIT, s.LowerLimit);
            Assert.AreEqual(MAX_CHANGE, s.MaximumRateOfChange);
        }

        [Test]
        public void SetterTests()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");

            s.Unit = "Q";
            Assert.AreEqual("Q", s.Unit);

            s.UpperLimit = 1000;
            Assert.AreEqual(1000, s.UpperLimit);

            s.LowerLimit = 20;
            Assert.AreEqual(20, s.LowerLimit);

            s.MaximumRateOfChange = 200;
            Assert.AreEqual(200, s.MaximumRateOfChange);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void LowerLimitHigherThanUpperLimitConstruct()
        {
            var s = new SensorTemplate(UNIT, LOWER_LIMIT, UPPER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains,
                                       "Temp");
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetLowerLimitGreaterThanUpperLimit()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");
            s.LowerLimit = 600;
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetUpperLimitLowerThanLowerLimit()
        {
            var s = new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp");
            s.UpperLimit = 0;
        }

        [Test]
        public void ExportsOnePresetCorrectly()
        {
            var templates = new List<SensorTemplate>{ new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp") };
            SensorTemplate.ExportAll(templates);

            Assert.AreEqual(SensorTemplateSingleXML, File.ReadAllText(SensorTemplate.ExportPath));
        }
    }
}
