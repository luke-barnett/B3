using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IndiaTango.Models
{
    public class Dataset
    {
        private Buoy _buoy;
        private DateTime _startTimeStamp;
        private DateTime _endTimeStamp;
        private List<Sensor> _sensors;

        /// <summary>
        /// Creates a new dataset with a specified start and end timestamp
        /// </summary>
        /// <param name="buoy">The buoy that the dataset came from</param>
        /// <param name="startTimeStamp">The start time of the dataset</param>
        /// <param name="endTimeStamp">The end time of the dataset</param>
        public Dataset(Buoy buoy)
        {
            _buoy = buoy;
            _startTimeStamp = DateTime.MinValue;
            _endTimeStamp = DateTime.MinValue;
            _sensors = new List<Sensor>();
        }

        /// <summary>
        /// Creates a new dataset from a list of sensors. Start and end timestamp will be dynamically created
        /// </summary>
        /// <param name="buoy">The buoy that the dataset came from</param>
        /// <param name="startTimeStamp">The start time of the dataset</param>
        /// <param name="endTimeStamp">The end time of the dataset</param>
        public Dataset(Buoy buoy, List<Sensor> sensors)
        {
            if (sensors == null)
                throw new ArgumentException("Please provide a list of sensors that belong to this site.");
            if (sensors.Count == 0)
                throw new ArgumentException("Sensor list must contain at least one sensor.");

            _buoy = buoy;
            Sensors = sensors;//To trigger setter
        }

        /// <summary>
        /// Gets and sets the buoy that this dataset came from
        /// </summary>
        public Buoy Buoy
        {
            get { return _buoy; }
            set { _buoy = value; }
        }

        /// <summary>
        /// Returns the start time stamp for this dataset
        /// </summary>
        public DateTime StartTimeStamp
        {
            get { return _startTimeStamp; }
        }

        /// <summary>
        /// Returns the end time stamp for this dataset
        /// </summary>
        public DateTime EndTimeStamp
        {
            get { return _endTimeStamp; }
        }

        /// <summary>
        /// Returns the list of sensors for this dataset
        /// </summary>
        public List<Sensor> Sensors
        {
        	get { return _sensors; }
        	set
        	{
                if (value == null)
                    throw new ArgumentException("Sensors cannot be null");

        	    _sensors = value;

                //Set the start and end time dynamically
        	    _startTimeStamp = DateTime.MinValue;
        	    _endTimeStamp = DateTime.MinValue;
                foreach (Sensor sensor in _sensors)
        	    {
                    if (sensor.CurrentState.Values.Count > 0)
                    {
                        if (_startTimeStamp == null || _startTimeStamp == DateTime.MinValue || sensor.CurrentState.Values[0].Timestamp < _startTimeStamp)
                            _startTimeStamp = sensor.CurrentState.Values[0].Timestamp;

                        if (_endTimeStamp == null || _startTimeStamp == DateTime.MinValue ||
                            sensor.CurrentState.Values[sensor.CurrentState.Values.Count - 1].Timestamp > _endTimeStamp)
                            _endTimeStamp = sensor.CurrentState.Values[sensor.CurrentState.Values.Count - 1].Timestamp;
                    }
        	    }
        	}
        }

    	/// <summary>
        /// Adds a sensor to the list of sensors
        /// </summary>
        /// <param name="sensor">The sensor to be added to the list of sensors</param>
        public void AddSensor(Sensor sensor)
        {
            if (sensor == null)
                throw new ArgumentException("Sensor cannot be null");
            _sensors.Add(sensor);
        }

        public int DataPointCount
        {
            get
            {
                //An additional +1 is added to the return value to account for the initial data point
                return (int)Math.Floor(EndTimeStamp.Subtract(StartTimeStamp).TotalMinutes/15) + 1;
            }
        }
    }
}