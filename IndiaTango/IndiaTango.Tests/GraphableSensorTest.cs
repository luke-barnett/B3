using System;
using System.Linq;
using System.Windows.Media;
using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    class GraphableSensorTest
    {
        private GraphableSensor _gSensor;
        private DateTime _baseDate;

        [SetUp]
        public void SetUp()
        {
            var rawSensor = new Sensor("Temperature", "Temperature at 30m", 40, 20, "C", 20, null);

            rawSensor.AddState(new SensorState(rawSensor, DateTime.Now));

            _baseDate = DateTime.Now;

            rawSensor.CurrentState.Values.Add(_baseDate, 15);
            rawSensor.CurrentState.Values.Add(_baseDate.AddMinutes(15), 20);
            rawSensor.CurrentState.Values.Add(_baseDate.AddMinutes(30), 25);

            _gSensor = new GraphableSensor(rawSensor);
        }

        [Test]
        public void ColourTesting()
        {
            Assert.IsNotNull(_gSensor.Colour);

            _gSensor.Colour = Colors.Black;

            Assert.AreEqual(_gSensor.Colour, Colors.Black);
        }

        [Test]
        public void GetSensor()
        {
            Assert.IsNotNull(_gSensor.Sensor);
        }

        [Test]
        public void DataPointsTest()
        {
            var dataPoints = _gSensor.DataPoints.ToArray();
            Assert.AreEqual(15, dataPoints[0].Y);
            Assert.AreEqual(20, dataPoints[1].Y);
            Assert.AreEqual(25, dataPoints[2].Y);
        }

        [Test]
        public void SetBoundsTest()
        {
            _gSensor.SetUpperAndLowerBounds(_baseDate.AddMinutes(10), _baseDate.AddMinutes(28));
            Assert.AreEqual(1, _gSensor.DataPoints.Count());
        }

        [Test]
        public void SetAndRemoveBoundsTest()
        {
            SetBoundsTest();
            _gSensor.RemoveBounds();
            Assert.AreEqual(3, _gSensor.DataPoints.Count());
        }

        [Test]
        public void GetUpperBoundsTest()
        {
            Assert.AreEqual(DateTime.MinValue, _gSensor.UpperBound);
            SetBoundsTest();
            Assert.AreEqual(_baseDate.AddMinutes(28), _gSensor.UpperBound);
            _gSensor.RemoveBounds();
            Assert.AreEqual(DateTime.MinValue, _gSensor.UpperBound);
        }

        [Test]
        public void GetLowerBoundsTest()
        {
            Assert.AreEqual(DateTime.MinValue, _gSensor.LowerBound);
            SetBoundsTest();
            Assert.AreEqual(_baseDate.AddMinutes(10), _gSensor.LowerBound);
            _gSensor.RemoveBounds();
            Assert.AreEqual(DateTime.MinValue, _gSensor.LowerBound);
        }

        [Test]
        public void GetBoundsSet()
        {
            Assert.AreEqual(false, _gSensor.BoundsSet);
            SetBoundsTest();
            Assert.AreEqual(true, _gSensor.BoundsSet);
            _gSensor.RemoveBounds();
            Assert.AreEqual(false, _gSensor.BoundsSet);
        }
    }
}
