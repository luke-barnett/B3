using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private IEnumerable<DataPoint<DateTime, float>> _dataPoints;
        private DateTime _lowerBound;
        private DateTime _upperBound;
        private bool _subSample;

        /// <summary>
        /// Creates a new GraphableSensor based on the given sensor
        /// </summary>
        /// <param name="baseSensor">The sensor to base it on</param>
        public GraphableSensor(Sensor baseSensor)
        {
            Sensor = baseSensor;
            //Create a random colour
            Colour = Color.FromRgb((byte)(Common.Generator.Next()), (byte)(Common.Generator.Next()), (byte)(Common.Generator.Next()));

            DataPoints = null;
        }

        /// <summary>
        /// Creates a new GraphableSensor based on the given Graphablesensor
        /// </summary>
        /// <param name="baseSensor">The sensor to base it on</param>
        /// <param name="lowerTimeBound">The lowest time value allowed</param>
        /// <param name="upperTimeBound">The highest time value allowed</param>
        public GraphableSensor(GraphableSensor baseSensor, DateTime lowerTimeBound, DateTime upperTimeBound)
        {
            Debug.WriteLine("Lower Time Bound {0} Upper Time Bound {1}", lowerTimeBound, upperTimeBound);

            Sensor = baseSensor.Sensor;
            Colour = baseSensor.Colour;

            _lowerBound = lowerTimeBound;
            _upperBound = upperTimeBound;

            _subSample = true;

            DataPoints = null;
        }

        /// <summary>
        /// The colour to use for the line series
        /// </summary>
        public Color Colour { get; set; }

        /// <summary>
        /// The sensor to base it all on
        /// </summary>
        public Sensor Sensor { get; private set; }

        /// <summary>
        /// The datapoints to use
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> DataPoints { get { if (_dataPoints == null) { RefreshDataPoints(); } return _dataPoints; } private set { _dataPoints = value; } }

        /// <summary>
        /// Reflects back on itself
        /// </summary>
        public GraphableSensor This { get { return this;}}

        /// <summary>
        /// Re extracts the data points from the sensor
        /// </summary>
        public void RefreshDataPoints()
        {
            DataPoints = !_subSample ? (from dataValue in Sensor.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X) : (from dataValue in Sensor.CurrentState.Values where dataValue.Key >= _lowerBound && dataValue.Key <= _upperBound select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X);
        }

        /// <summary>
        /// Changes the sensor to take a particular subsample of their data source
        /// </summary>
        /// <param name="lowerBound">The lower bound</param>
        /// <param name="upperBound">The upper bound</param>
        public void SetUpperAndLowerBounds(DateTime lowerBound, DateTime upperBound)
        {
            _lowerBound = lowerBound;
            _upperBound = upperBound;
            _subSample = true;

            //Force it to be recalculated
            DataPoints = null;
        }

        /// <summary>
        /// Removes the sub sample bounds
        /// </summary>
        public void RemoveBounds()
        {
            _subSample = false;
            //Force it to be recalculated
            DataPoints = null;
        }

        /// <summary>
        /// Overrides the ToString Method to show the ToString method of the Sensor
        /// </summary>
        /// <returns>The ToString result from the sensor</returns>
        public override string ToString()
        {
            return Sensor.ToString();
        }
    }
}
