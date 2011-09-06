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

        [SetUp]
        public void SetUp()
        {
            var rawSensor = new Sensor("Temperature", "Temperature at 30m", 40, 20, "C", 20, "Temperature Sensors Ltd.", "1102123");

            rawSensor.AddState(new SensorState(DateTime.Now));
            rawSensor.CurrentState.Values.Add(new DataValue(DateTime.Now,15));
            rawSensor.CurrentState.Values.Add(new DataValue(DateTime.Now, 20));
            rawSensor.CurrentState.Values.Add(new DataValue(DateTime.Now, 25));

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
            Assert.AreEqual(dataPoints[0].Y, 15);
            Assert.AreEqual(dataPoints[1].Y, 20);
            Assert.AreEqual(dataPoints[2].Y, 25);
        }
    }
}
