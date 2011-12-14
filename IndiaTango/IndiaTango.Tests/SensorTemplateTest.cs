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
            "<ArrayOfSensorTemplate xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><SensorTemplate><AUpperLimit>500</AUpperLimit><LowerLimit>200</LowerLimit><MatchingStyle>Contains</MatchingStyle><MaximumRateOfChange>10</MaximumRateOfChange><Pattern>Temp</Pattern><Unit>C</Unit></SensorTemplate></ArrayOfSensorTemplate>";
        private const string SensorTemplateDualXML =
            "<ArrayOfSensorTemplate xmlns=\"http://schemas.datacontract.org/2004/07/IndiaTango.Models\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><SensorTemplate><AUpperLimit>500</AUpperLimit><LowerLimit>200</LowerLimit><MatchingStyle>Contains</MatchingStyle><MaximumRateOfChange>10</MaximumRateOfChange><Pattern>Temp</Pattern><Unit>C</Unit></SensorTemplate><SensorTemplate><AUpperLimit>1000</AUpperLimit><LowerLimit>700</LowerLimit><MatchingStyle>StartsWith</MatchingStyle><MaximumRateOfChange>7</MaximumRateOfChange><Pattern>Humid</Pattern><Unit>Q</Unit></SensorTemplate></ArrayOfSensorTemplate>";

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
            var testSensor = new Sensor("Temperature", "C", 150, 50, "F", 8.3f, null);
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

            s.Pattern = "X";
            Assert.AreEqual("X", s.Pattern);
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

        [Test]
        public void ExportsTwoPresetsCorrectly()
        {
            var templates = new List<SensorTemplate> { new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp"), new SensorTemplate("Q", 1000, 700, 7, SensorTemplate.MatchStyle.StartsWith, "Humid") };
            SensorTemplate.ExportAll(templates);

            Assert.AreEqual(SensorTemplateDualXML, File.ReadAllText(SensorTemplate.ExportPath));
        }

        [Test]
        public void ImportsOnePresetCorrectly()
        {
            File.WriteAllText(SensorTemplate.ExportPath, SensorTemplateSingleXML);

            var templates = new List<SensorTemplate> { new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp") };
            var result = SensorTemplate.ImportAll();

            Assert.AreEqual(templates, result);
        }

        [Test]
        public void ImportsTwoPresetsCorrectly()
        {
            File.WriteAllText(SensorTemplate.ExportPath, SensorTemplateDualXML);

            var templates = new List<SensorTemplate> { new SensorTemplate(UNIT, UPPER_LIMIT, LOWER_LIMIT, MAX_CHANGE, SensorTemplate.MatchStyle.Contains, "Temp"), new SensorTemplate("Q", 1000, 700, 7, SensorTemplate.MatchStyle.StartsWith, "Humid") };
            var result = SensorTemplate.ImportAll();

            Assert.AreEqual(templates, result);
        }

        [Test]
        public void ImportWhenFileNotFound()
        {
            File.Delete(SensorTemplate.ExportPath);
            var result = SensorTemplate.ImportAll();

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void EqualityTest()
        {
            var A = new SensorTemplate("C", 100, 0, 1, SensorTemplate.MatchStyle.Contains, "Test");
            var B = new SensorTemplate("C", 100, 0, 1, SensorTemplate.MatchStyle.Contains, "Test");
            var C = new SensorTemplate("Q", 100, 0, 1, SensorTemplate.MatchStyle.StartsWith, "Test");

            Assert.AreEqual(A, B);

            Assert.IsFalse(A.Equals(null));
            Assert.IsFalse(A.Equals(C));
        }

        [Test]
        public void ToStringTest()
        {
            var A = new SensorTemplate("C", 100, 0, 1, SensorTemplate.MatchStyle.Contains, "Test");
            var B = new SensorTemplate("C", 100, 0, 1, SensorTemplate.MatchStyle.EndsWith, "Test");
            var C = new SensorTemplate("Q", 100, 0, 1, SensorTemplate.MatchStyle.StartsWith, "Test");

            Assert.AreEqual("Match if contains 'Test'", A.ToString());
            Assert.AreEqual("Match if ends with 'Test'", B.ToString());
            Assert.AreEqual("Match if starts with 'Test'", C.ToString());

        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptyPatternTest()
        {
            var A = new SensorTemplate("C", 100, 0, 1, SensorTemplate.MatchStyle.Contains, "Test");
            A.Pattern = null;
        }
    }
}
