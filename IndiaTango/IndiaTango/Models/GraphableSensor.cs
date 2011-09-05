using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Wraps a sensor with some needed graph data
    /// </summary>
    public class GraphableSensor
    {
        /// <summary>
        /// Creates a new GraphableSensor based on the given sensor
        /// </summary>
        /// <param name="baseSensor">The sensor to base it on</param>
        public GraphableSensor(Sensor baseSensor)
        {
            Sensor = baseSensor;
            //Create a random colour
            var generator = new Random();
            Colour = new SolidColorBrush(Color.FromRgb((byte)generator.Next(255), (byte)generator.Next(255), (byte)generator.Next(255)));

            DataPoints = from dataValue in Sensor.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value);
        }

        /// <summary>
        /// The colour to use for the line series
        /// </summary>
        public Brush Colour { get; set; }

        /// <summary>
        /// The sensor to base it all on
        /// </summary>
        public Sensor Sensor { get; private set; }

        /// <summary>
        /// The datapoints to use
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> DataPoints { get; private set; }

        /// <summary>
        /// Re extracts the data points from the sensor
        /// </summary>
        public void RefreshDataPoints()
        {
            DataPoints = from dataValue in Sensor.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value);
        }
    }
}
