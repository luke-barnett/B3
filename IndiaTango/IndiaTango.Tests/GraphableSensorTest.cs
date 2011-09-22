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
        private GraphableSensor sensor;
        private DateTime baseDate;

        [SetUp]
        public void SetUp()
        {
            var rawSensor = new Sensor("Temperature", "Temperature at 30m", 40, 20, "C", 20, "Temperature Sensors Ltd.", "1102123", null);

            rawSensor.AddState(new SensorState(DateTime.Now));

            baseDate = DateTime.Now;

            rawSensor.CurrentState.Values.Add(baseDate, 15);
            rawSensor.CurrentState.Values.Add(baseDate.AddMinutes(15), 20);
            rawSensor.CurrentState.Values.Add(baseDate.AddMinutes(30), 25);

            sensor = new GraphableSensor(rawSensor);
        }
        
        [Test]
        public void ColourTesting()
        {
            Assert.IsNotNull(sensor.Colour);

            sensor.Colour = Colors.Black;

            Assert.AreEqual(sensor.Colour, Colors.Black);
        }

        [Test]
        public void GetSensor()
        {
            Assert.IsNotNull(sensor.Sensor);
        }

        [Test]
        public void DataPointsTest()
        {
            var dataPoints = sensor.DataPoints.ToArray();
            Assert.AreEqual(15, dataPoints[0].Y);
            Assert.AreEqual(20, dataPoints[1].Y);
            Assert.AreEqual(25, dataPoints[2].Y);
        }

        [Test]
        public void SetBoundsTest()
        {
            sensor.SetUpperAndLowerBounds(baseDate.AddMinutes(10), baseDate.AddMinutes(28));
            Assert.AreEqual(1, sensor.DataPoints.Count());
        }

        [Test]
        public void SetAndRemoveBoundsTest()
        {
            SetBoundsTest();
            sensor.RemoveBounds();
            Assert.AreEqual(3, sensor.DataPoints.Count());
        }

        [Test]
        public void GetUpperBoundsTest()
        {
            Assert.AreEqual(DateTime.MinValue, sensor.UpperBound);
            SetBoundsTest();
            Assert.AreEqual(baseDate.AddMinutes(28), sensor.UpperBound);
            sensor.RemoveBounds();
            Assert.AreEqual(DateTime.MinValue, sensor.UpperBound);
        }

        [Test]
        public void GetLowerBoundsTest()
        {
            Assert.AreEqual(DateTime.MinValue, sensor.LowerBound);
            SetBoundsTest();
            Assert.AreEqual(baseDate.AddMinutes(10), sensor.LowerBound);
            sensor.RemoveBounds();
            Assert.AreEqual(DateTime.MinValue, sensor.LowerBound);
        }

        [Test]
        public void GetBoundsSet()
        {
            Assert.AreEqual(false, sensor.BoundsSet);
            SetBoundsTest();
            Assert.AreEqual(true, sensor.BoundsSet);
            sensor.RemoveBounds();
            Assert.AreEqual(false, sensor.BoundsSet);
        }
    }
}
