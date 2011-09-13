using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace IndiaTango.Models
{
    public class Dataset
    {
        private Site _site;
        private DateTime _startTimeStamp;
        private DateTime _endTimeStamp;
        private List<Sensor> _sensors;
        private int _dataPointCount = 0;
        private int _dataInterval;

        /// <summary>
        /// Creates a new dataset with a specified start and end timestamp
        /// </summary>
        /// <param name="site">The Site that the dataset came from</param>
        public Dataset(Site site)
        {
            _site = site;
            _startTimeStamp = DateTime.MinValue;
            _endTimeStamp = DateTime.MinValue;
            _sensors = new List<Sensor>();
        }

        /// <summary>
        /// Creates a new dataset from a list of sensors. Start and end timestamp will be dynamically created
        /// </summary>
        /// <param name="site">The Site that the dataset came from</param>
        public Dataset(Site site, List<Sensor> sensors)
        {
            if (sensors == null)
                throw new ArgumentException("Please provide a list of sensors that belong to this site.");
            if (sensors.Count == 0)
                throw new ArgumentException("Sensor list must contain at least one sensor.");

            _site = site;
            Sensors = sensors;//To trigger setter
        }

        /// <summary>
        /// Gets and sets the Site that this dataset came from
        /// </summary>
        public Site Site
        {
            get { return _site; }
            set { _site = value; }
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


                if(Sensors[0] != null && Sensors[0].CurrentState != null)
                {
                    var intervalMap = new Dictionary<int, int>();
                    var prevDate = DateTime.MinValue;
                    var currentHighest = new KeyValuePair<int, int>();


                    foreach (var date in Sensors[0].CurrentState.Values.Keys)
                    {
                        var interval = (int)(date - prevDate).TotalMinutes;
                        
                        if (intervalMap.ContainsKey(interval))
                            intervalMap[interval]++;
                        else
                            intervalMap.Add(interval,1);

                        prevDate = date;
                    }

                    foreach (var pair in intervalMap)
                        if (pair.Value > currentHighest.Value)
                            currentHighest = pair;

                    _dataInterval = currentHighest.Key;
                }

                foreach (Sensor sensor in _sensors)
                {
                    //Update the datapointcount
                    if(sensor.CurrentState != null)
                        _dataPointCount = Math.Max(sensor.CurrentState.Values.Count, _dataPointCount);

                    //Set the start and end time dynamically
                    if (sensor.CurrentState != null && sensor.CurrentState.Values.Count > 0)
                    {
                        var timesArray = new DateTime[sensor.CurrentState.Values.Count];
                        sensor.CurrentState.Values.Keys.CopyTo(timesArray, 0);
                        var timesList = new List<DateTime>(timesArray);
                        timesList.Sort();

                        if (_startTimeStamp == null || _startTimeStamp == DateTime.MinValue ||
                            timesList[0] < _startTimeStamp)
                            _startTimeStamp = timesList[0];

                        if (_endTimeStamp == null || _startTimeStamp == DateTime.MinValue ||
                            timesList[timesList.Count - 1] >
                            _endTimeStamp)
                            _endTimeStamp =
                                timesList[timesList.Count - 1];
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

            // Force an update of the start and end timestamps - very important!
    	    var sensorList = Sensors;
    	    sensorList.Add(sensor);
    	    Sensors = sensorList;
        }

        /// <summary>
        /// Returns the time interval in minutes the data points are placed at
        /// </summary>
        public int DataInterval
        {
            get { return _dataInterval; }
        }

        public int DataPointCount
        {
            get
            {
                //An additional +1 is added to the return value to account for the initial data point
                //return (int)Math.Floor(EndTimeStamp.Subtract(StartTimeStamp).TotalMinutes/15) + 1;
                return _dataPointCount;
            }
        }
    }
}