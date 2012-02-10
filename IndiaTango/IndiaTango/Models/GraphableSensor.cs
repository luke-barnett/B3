using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Wraps a sensor with some needed graph data
    /// </summary>
    public class GraphableSensor : INotifyPropertyChanged
    {
        private IEnumerable<DataPoint<DateTime, float>> _dataPoints;
        private IEnumerable<DataPoint<DateTime, float>> _rawDataPoints;
        private IEnumerable<DataPoint<DateTime, float>> _upperLimit;
        private IEnumerable<DataPoint<DateTime, float>> _lowerLimit;

        /// <summary>
        /// Creates a new GraphableSensor based on the given sensor
        /// </summary>
        /// <param name="baseSensor">The sensor to base it on</param>
        public GraphableSensor(Sensor baseSensor)
        {
            Sensor = baseSensor;

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

            LowerBound = lowerTimeBound;
            UpperBound = upperTimeBound;

            BoundsSet = true;

            DataPoints = null;
        }

        /// <summary>
        /// The colour to use for the line series
        /// </summary>
        public Color Colour { get { return Sensor.Colour; } set { Sensor.Colour = value; } }

        /// <summary>
        /// The colour used to plot raw values
        /// </summary>
        public Color RawDataColour
        {
            get
            {
                return Color.FromArgb(Colour.A, (byte)~Colour.R, (byte)~Colour.G, (byte)~Colour.B);
            }
        }

        /// <summary>
        /// The sensor to base it all on
        /// </summary>
        public Sensor Sensor { get; private set; }

        /// <summary>
        /// The datapoints to use
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> DataPoints { get { if (_dataPoints == null) { RefreshDataPoints(); } return _dataPoints; } private set { _dataPoints = value; } }

        /// <summary>
        /// The lower line to graph
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> LowerLine { get { if (_lowerLimit == null) { RefreshDataPoints(); } return _lowerLimit; } private set { _lowerLimit = value; } }

        /// <summary>
        /// The upper line to graph
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> UpperLine { get { if (_upperLimit == null) { RefreshDataPoints(); } return _upperLimit; } private set { _upperLimit = value; } }

        /// <summary>
        /// The set of raw data points
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> RawDataPoints { get { if (_rawDataPoints == null) { RefreshDataPoints(); } return _rawDataPoints; } private set { _rawDataPoints = value; } }

        /// <summary>
        /// The set of preview data points
        /// </summary>
        public IEnumerable<DataPoint<DateTime, float>> PreviewDataPoints { get; private set; }

        /// <summary>
        /// Reflects back on itself
        /// </summary>
        public GraphableSensor This { get { return this; } }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }

        /// <summary>
        /// Re extracts the data points from the sensor
        /// </summary>
        public void RefreshDataPoints()
        {
            PreviewDataPoints = null;
            DataPoints = !BoundsSet ? (from dataValue in Sensor.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X) : (from dataValue in Sensor.CurrentState.Values where dataValue.Key >= LowerBound && dataValue.Key <= UpperBound select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X);

            RawDataPoints = !BoundsSet ? (from dataValue in Sensor.RawData.Values select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X) : (from dataValue in Sensor.RawData.Values where dataValue.Key >= LowerBound && dataValue.Key <= UpperBound select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X);

            if (Sensor.CurrentState.UpperLine == null) return;
            LowerLine = !BoundsSet ? (from dataValue in Sensor.CurrentState.LowerLine select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X) : (from dataValue in Sensor.CurrentState.LowerLine where dataValue.Key >= LowerBound && dataValue.Key <= UpperBound select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X);
            UpperLine = !BoundsSet ? (from dataValue in Sensor.CurrentState.UpperLine select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X) : (from dataValue in Sensor.CurrentState.UpperLine where dataValue.Key >= LowerBound && dataValue.Key <= UpperBound select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X);


        }

        /// <summary>
        /// Changes the sensor to take a particular subsample of their data source
        /// </summary>
        /// <param name="lowerBound">The lower bound</param>
        /// <param name="upperBound">The upper bound</param>
        public void SetUpperAndLowerBounds(DateTime lowerBound, DateTime upperBound)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
            BoundsSet = true;

            //Force it to be recalculated
            DataPoints = null;
        }

        /// <summary>
        /// Removes the sub sample bounds
        /// </summary>
        public void RemoveBounds()
        {
            BoundsSet = false;
            //Force it to be recalculated
            DataPoints = null;
            //Reset the bounds
            LowerBound = DateTime.MinValue;
            UpperBound = DateTime.MinValue;
        }

        /// <summary>
        /// Overrides the ToString Method to show the ToString method of the Sensor
        /// </summary>
        /// <returns>The ToString result from the sensor</returns>
        public override string ToString()
        {
            return "";
        }

        /// <summary>
        /// Are there bounds set on the data values?
        /// </summary>
        public bool BoundsSet { get; private set; }

        /// <summary>
        /// The lower bound of the data values
        /// </summary>
        public DateTime LowerBound { get; private set; }

        /// <summary>
        /// The upper bound of the data values
        /// </summary>
        public DateTime UpperBound { get; private set; }

        /// <summary>
        /// Creates a preview of the state given
        /// </summary>
        /// <param name="stateToPreview">The state to preview</param>
        public void GeneratePreview(SensorState stateToPreview)
        {
            PreviewDataPoints = !BoundsSet ? (from dataValue in stateToPreview.Values select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X) : (from dataValue in stateToPreview.Values where dataValue.Key >= LowerBound && dataValue.Key <= UpperBound select new DataPoint<DateTime, float>(dataValue.Key, dataValue.Value)).OrderBy(dataPoint => dataPoint.X);
        }

        /// <summary>
        /// Removes the preview data points
        /// </summary>
        public void RemovePreview()
        {
            PreviewDataPoints = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
