using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorTemplateTest
    {
        private Sensor _sensor;
        private string _pattern = "Temp";

        [SetUp]
        public void SetUp()
        {
            _sensor = new Sensor("Temperature", "C");
        }

        [Test]
        public void ConstructionWithValidSensor()
        {
            var s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.Contains, _pattern);
            Assert.Pass();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructionWithNullSensor()
        {
            var s = new SensorTemplate(null, SensorTemplate.MatchStyle.Contains, _pattern);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructionWithEmptyPattern()
        {
            var s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.Contains, "");
        }

        [Test]
        public void SensorNameMatchedCorrectly()
        {
            var testSensor = new Sensor("Temperature", "C");

            var s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.Contains, "Temp");
            Assert.IsTrue(s.Matches(testSensor));

            s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.StartsWith, "Temp");
            Assert.IsTrue(s.Matches(testSensor));

            s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.EndsWith, "Temp");
            Assert.IsFalse(s.Matches(testSensor));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException), ExpectedMessage="Cannot assign template values to a null sensor.")]
        public void ProvideDefaultValuesNullSensor()
        {
            var s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.Contains, "Temp");
            s.ProvideDefaultValues(null);
        }

        [Test]
        public void ProvidesDefaultValuesWhenMatches()
        {
            var testSensor = new Sensor("Temperature", "C", 150, 50, "F", 8.3f, "Random Corporation", "ASA932832");
            var s = new SensorTemplate(_sensor, SensorTemplate.MatchStyle.Contains, "Temp");

            s.ProvideDefaultValues(testSensor);

            Assert.AreEqual(testSensor.Unit, s.Unit);
            Assert.AreEqual(testSensor.UpperLimit, s.UpperLimit);
            Assert.AreEqual(testSensor.LowerLimit, s.LowerLimit);
            Assert.AreEqual(testSensor.MaxRateOfChange, s.MaximumRateOfChange);
        }
    }
}
