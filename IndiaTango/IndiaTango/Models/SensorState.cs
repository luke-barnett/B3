using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// A class representing a sensor state; that is, the values a sensor held at a given instance in time.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class SensorState
    {
        private DateTime _editTimestamp;
        private Dictionary<DateTime, float> _valueList;
        private ChangeReason _reason;
        private Dictionary<DateTime, float> _upperLine;
        private Dictionary<DateTime, float> _lowerLine;
        private Dictionary<DateTime, LinkedList<int>> _changes;
        private bool _isRaw;
        [ProtoMember(6, AsReference = true)]
        private readonly Sensor _owner;

        public SensorState Clone()
        {
            var d = _valueList.ToDictionary(v => v.Key, v => v.Value);
            var c = _changes.ToDictionary(v => v.Key, v => v.Value);
            var s = new SensorState(_owner, d, c);

            return s;
        }

        #region Constructors

        public SensorState()
        {
            _changes = new Dictionary<DateTime, LinkedList<int>>();
            _valueList = new Dictionary<DateTime, float>();
        }

        public SensorState(Sensor owner, Dictionary<DateTime, float> valueList, ChangeReason reason, Dictionary<DateTime, LinkedList<int>> changes) : this(owner, DateTime.Now, valueList, reason, false, changes) { }
        public SensorState(Sensor owner, Dictionary<DateTime, float> valueList, Dictionary<DateTime, LinkedList<int>> changes) : this(owner, DateTime.Now, valueList, changes) { }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited.
        /// </summary>
        /// <param name="owner">The sensor that this state belongs to</param>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        public SensorState(Sensor owner, DateTime editTimestamp)
            : this(owner, editTimestamp, new Dictionary<DateTime, float>(), new Dictionary<DateTime, LinkedList<int>>())
        {
        }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, and a list of values representing data values recorded in this state.
        /// </summary>
        /// <param name="owner">The sensor that this state belongs to</param>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        /// <param name="changes">The list of changes that have occured for this sensor</param>
        public SensorState(Sensor owner, DateTime editTimestamp, Dictionary<DateTime, float> valueList, Dictionary<DateTime, LinkedList<int>> changes) : this(owner, editTimestamp, valueList, null, false, changes) { }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, a list of values representing data values recorded in this state, and a reason for the changes stored in this state.
        /// </summary>
        /// <param name="owner">The sensor the sensor state belongs to</param>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        /// <param name="reason">A string indicating the reason for the changes made in this state.</param>
        /// <param name="isRaw">Whether or not this represents the sensors raw data.</param>
        /// <param name="changes">The list of changes that have occured for this sensor</param>
        public SensorState(Sensor owner, DateTime editTimestamp, Dictionary<DateTime, float> valueList, ChangeReason reason, bool isRaw, Dictionary<DateTime, LinkedList<int>> changes)
        {
            if (valueList == null)
                throw new ArgumentNullException("valueList");

            _reason = reason;
            _editTimestamp = editTimestamp;
            _valueList = valueList;
            _isRaw = isRaw;
            _changes = changes ?? new Dictionary<DateTime, LinkedList<int>>();
            _owner = owner;
        }

        #endregion

        /// <summary>
        /// The list of changes made to the sensor state from the original raw values
        /// </summary>
        [ProtoMember(1)]
        public Dictionary<DateTime, LinkedList<int>> Changes
        {
            get { return _changes; }
            set { _changes = value; }
        }

        /// <summary>
        /// Whether or not this state is the raw values state
        /// </summary>
        [ProtoMember(2)]
        public bool IsRaw
        {
            get { return _isRaw; }
            private set { _isRaw = value; }
        }

        /// <summary>
        /// Gets or sets the timestamp of the last edit to this sensor state.
        /// </summary>
        [ProtoMember(3)]
        public DateTime EditTimestamp
        {
            get { return _editTimestamp; }
            set { _editTimestamp = value; }
        }

        /// <summary>
        /// Gets or sets the list of values this sensor state holds.
        /// </summary>
        public Dictionary<DateTime, float> Values
        {
            get { return _valueList; }
            set { _valueList = value; }
        }

        /// <summary>
        /// The list of values in a compressed state
        /// </summary>
        public DataBlock[] CompressedValues
        {
            get
            {
                var dataBlocks = new List<DataBlock>();
                if (Values != null)
                {
                    var orderedKeyValuePairs = Values.OrderBy(x => x.Key).ToArray();

                    var i = 0;
                    while (i < orderedKeyValuePairs.Length)
                    {
                        var dataBlock = new DataBlock
                                            {
                                                DataInterval = _owner.Owner.DataInterval,
                                                StartTime = orderedKeyValuePairs[i].Key
                                            };
                        var values = new List<float> { orderedKeyValuePairs[i].Value };
                        var j = 0;
                        while (i + j + 1 < orderedKeyValuePairs.Length &&
                               orderedKeyValuePairs[i + j + 1].Key - orderedKeyValuePairs[i + j].Key ==
                               new TimeSpan(0, dataBlock.DataInterval, 0))
                        {
                            values.Add(orderedKeyValuePairs[i + j + 1].Value);
                            j++;
                        }

                        dataBlock.Values = values.ToArray();
                        dataBlocks.Add(dataBlock);
                        i += j + 1;
                    }

                    //dataBlocks.ForEach(x => Debug.WriteLine(x));
                }
                return dataBlocks.ToArray();
            }
            set
            {
                var dictionary = new Dictionary<DateTime, float>();
                foreach (var block in value)
                {
                    for (var i = 0; i < block.Values.Length; i++)
                    {
                        dictionary[block.StartTime.AddMinutes(i * block.DataInterval)] = block.Values[i];
                    }
                }
                Values = dictionary;
            }

        }

        /// <summary>
        /// Gets a set of compressed values
        /// </summary>
        /// <param name="inclusiveStartTimestamp">The start of the values to get</param>
        /// <param name="exclusiveEndTimestamp">The exclusive end of the values to get</param>
        /// <returns>A compressed data block of values</returns>
        public DataBlock[] GetCompressedValues(DateTime inclusiveStartTimestamp, DateTime exclusiveEndTimestamp)
        {
            var dataBlocks = new List<DataBlock>();
            if (Values != null)
            {
                var orderedKeyValuePairs = Values.Where(x => x.Key >= inclusiveStartTimestamp && x.Key < exclusiveEndTimestamp).OrderBy(x => x.Key).ToArray();

                var i = 0;
                while (i < orderedKeyValuePairs.Length)
                {
                    var dataBlock = new DataBlock
                    {
                        DataInterval = _owner.Owner.DataInterval,
                        StartTime = orderedKeyValuePairs[i].Key
                    };
                    var values = new List<float> { orderedKeyValuePairs[i].Value };
                    var j = 0;
                    while (i + j + 1 < orderedKeyValuePairs.Length &&
                           orderedKeyValuePairs[i + j + 1].Key - orderedKeyValuePairs[i + j].Key ==
                           new TimeSpan(0, dataBlock.DataInterval, 0))
                    {
                        values.Add(orderedKeyValuePairs[i + j + 1].Value);
                        j++;
                    }

                    dataBlock.Values = values.ToArray();
                    dataBlocks.Add(dataBlock);
                    i += j + 1;
                }
            }
            return dataBlocks.ToArray();
        }

        /// <summary>
        /// Adds a block of compressed values to the list of values
        /// </summary>
        /// <param name="values">The data block to add</param>
        public void AddCompressedValues(DataBlock[] values)
        {
            if(values == null)
                return;
            foreach (var block in values)
            {
                for (var i = 0; i < block.Values.Length; i++)
                {
                    Values[block.StartTime.AddMinutes(i * block.DataInterval)] = block.Values[i];
                }
            }
        }

        /// <summary>
        /// The reason for the change made to this sensor state
        /// </summary>
        [ProtoMember(5)]
        public ChangeReason Reason
        {
            get { return _reason; }
            set { _reason = value; }
        }

        /// <summary>
        /// Determines whether a given object is equal to this SensorState object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>Whether or not the given object, and this SensorState, are equal.</returns>
        public override bool Equals(object obj) // TODO: test this
        {
            if (!(obj is SensorState))
                return false;

            var s = obj as SensorState;

            if ((s.EditTimestamp - EditTimestamp).TotalMilliseconds > 0.00001)
                return false;

            return s.Values.Count == Values.Count && _valueList.All(f => s.Values.ContainsKey(f.Key) && s.Values[f.Key].Equals(f.Value));
        }

        /// <summary>
        /// Finds all the missing timestamps for this sensor state
        /// </summary>
        /// <param name="timeGap">The expected data interval</param>
        /// <param name="start">The timestamp to start looking from</param>
        /// <param name="end">The end timestamp to stop looking from</param>
        /// <returns>The list of missing timestamps</returns>
        public List<DateTime> GetMissingTimes(int timeGap, DateTime start, DateTime end)
        {
            var missing = new List<DateTime>();
            for (var time = start; time <= end; time = time.AddMinutes(timeGap))
            {
                if (!Values.ContainsKey(time))
                {
                    missing.Add(time);
                }
            }
            return missing;
        }

        /// <summary>
        /// Gets the outliers based on the limits set
        /// </summary>
        /// <param name="timeGap">The expected data interval</param>
        /// <param name="start">The start timestamp</param>
        /// <param name="end">The end timestamp</param>
        /// <param name="upperLimit">The upper limit</param>
        /// <param name="lowerLimit">The lower limit</param>
        /// <param name="maxRateChange">The maximum rate of change</param>
        /// <returns></returns>
        public List<DateTime> GetOutliersFromMaxAndMin(int timeGap, DateTime start, DateTime end, float upperLimit, float lowerLimit, float maxRateChange)
        {
            var outliers = new List<DateTime>();
            _upperLine = new Dictionary<DateTime, float> { { start, upperLimit }, { end, upperLimit } };
            _lowerLine = new Dictionary<DateTime, float> { { start, lowerLimit }, { end, lowerLimit } };

            var prev = 0f;
            for (var time = start; time <= end; time = time.AddMinutes(timeGap))
            {
                float value;
                if (!Values.TryGetValue(time, out value)) continue;
                if (value < lowerLimit || value > upperLimit)
                    outliers.Add(time);
                else if (time != start && Math.Abs(value - prev) > maxRateChange)
                    outliers.Add(time);
                prev = value;
            }
            return outliers;
        }

        /// <summary>
        /// Gets the outliers by calculating standard deviations for points
        /// </summary>
        /// <param name="timeGap">the time between each data point</param>
        /// <param name="start">the start time for the dataset</param>
        /// <param name="end">the end time for the dataset</param>
        /// <param name="numStdDev">the numnber of standard deviations from the mean that the outliers begin</param>
        /// <param name="smoothingPeriod">the number of points used to calculate the standard deviation</param>
        /// <returns></returns>
        public List<DateTime> GetOutliersFromStdDev(int timeGap, DateTime start, DateTime end, float numStdDev, int smoothingPeriod)
        {
            var outliers = new List<DateTime>();
            var values = new LinkedList<float>();
            _upperLine = new Dictionary<DateTime, float>();
            _lowerLine = new Dictionary<DateTime, float>();
            for (var i = start.AddMinutes(-(timeGap * (smoothingPeriod / 2)));
                 i < start.AddMinutes((timeGap * (smoothingPeriod / 2)));
                 i = i.AddMinutes(timeGap))
            {
                float value;
                value = (Values.TryGetValue(i, out value) ? value : float.NaN);
                values.AddLast(value);
            }
            for (var time = start; time <= end; time = time.AddMinutes(timeGap))
            {
                values.RemoveFirst();
                float next;
                next = (Values.TryGetValue(time.AddMinutes(timeGap * (smoothingPeriod / 2)), out next) ? next : float.NaN);
                values.AddLast(next);
                float value;
                value = (Values.TryGetValue(time, out value) ? value : float.NaN);

                if (float.IsNaN(value)) continue;

                var avg = GetAverage(values);
                var sum = GetSquaresSum(values, avg);
                var stdDev = (float)Math.Sqrt((sum) / (GetCount(values) - 1));
                var top = avg + (numStdDev * stdDev);
                var bottom = avg - (numStdDev * stdDev);
                if (value > top || value < bottom)
                {
                    outliers.Add(time);
                }
                //Debug.WriteLine("avg " + avg + " stddev " + stdDev + " top " + top + " bottom " + bottom);
                if (!float.IsNaN(top) && !float.IsNaN(bottom))
                {
                    _upperLine.Add(time, top);
                    _lowerLine.Add(time, bottom);
                }
            }
            return outliers;
        }

        /// <summary>
        /// Calculates the squares sum for a set of values
        /// </summary>
        /// <param name="values">The values to calculate from</param>
        /// <param name="average">The average of the values</param>
        /// <returns>The squares sum value</returns>
        private float GetSquaresSum(IEnumerable<float> values, float average)
        {
            return values.Where(value => !float.IsNaN(value)).Sum(v => (float)Math.Pow((v - average), 2));
        }

        /// <summary>
        /// Calculates how many real values there are in a list of values
        /// </summary>
        /// <param name="values">The list of values to look in</param>
        /// <returns>How many real values there are</returns>
        private int GetCount(IEnumerable<float> values)
        {
            return values.Count(value => !float.IsNaN(value));
        }

        /// <summary>
        /// Calculates the average of a list of values
        /// </summary>
        /// <param name="values">The list of values to calculate for</param>
        /// <returns>The average of the values</returns>
        private float GetAverage(IEnumerable<float> values)
        {
            var sum = 0f;
            var count = 0;
            foreach (var value in values.Where(value => !float.IsNaN(value)))
            {
                count++;
                sum += value;
            }
            return sum / count;
        }

        /// <summary>
        /// Returns a calibrated state
        /// </summary>
        /// <param name="start">The start timestamp for the calibration</param>
        /// <param name="end">The end timestamp for the calibration</param>
        /// <param name="origA">The original high</param>
        /// <param name="origB">The original low</param>
        /// <param name="newA">The calibrated high</param>
        /// <param name="newB">The calibrated low</param>
        /// <param name="reason">The reason for the calibration</param>
        /// <returns>The calibrated sensor state</returns>
        public SensorState Calibrate(DateTime start, DateTime end, double origA, double origB, double newA, double newB, ChangeReason reason)
        {
            if (start >= end)
                throw new ArgumentException("End time must be greater than start time");

            if (origB > origA)
                throw new ArgumentException("Calibrated B value must be less than the calibrated A value");

            if (newB > newA)
                throw new ArgumentException("Current B value must be less than the current A value");

            //The total minutes in the time span
            double totalMinutes = end.Subtract(start).TotalMinutes;

            //The distance from the median line to the A and B points
            double origMedianDistance = (origA - origB) / 2;
            double newMedianDistance = (newA - newB) / 2;

            //The largest distance the points must move to normalise them with the median line
            double normalisationDistance = newMedianDistance - origMedianDistance;

            //The largest distance the points must move from the normalised position, to the calibrated (correct) position
            double calibrationDistance = (newB + newMedianDistance) - (origB + origMedianDistance);

            //The increment that must be made per minute to normalise the points above and below the median line
            double normalisationIncrement = normalisationDistance / totalMinutes;
            double calibrationIncrement = calibrationDistance / totalMinutes;

            //The gradients of the lines
            double slopeA = (newA - origA) / totalMinutes;
            double slopeMedian = ((newB + newMedianDistance) - (origB + origMedianDistance)) / totalMinutes;

            //The y-intercepts of the lines
            double interceptA = origA;
            double interceptMedian = origB + origMedianDistance;

            //Copy!
            SensorState newState = Clone();

            foreach (var value in Values)
            {
                if (value.Key < start || value.Key > end)
                    continue;

                double timeDiff = value.Key.Subtract(start).TotalMinutes;
                double normalisationScale = GetRelativePositionBetweenLines(interceptA, interceptMedian, slopeA, slopeMedian, timeDiff, value.Value);

                newState.Values[value.Key] = (float)(value.Value - (normalisationScale * normalisationIncrement + calibrationIncrement) * timeDiff);
                newState.AddToChanges(value.Key, reason.ID);
            }

            return newState;
        }

        /// <summary>
        /// Gets the relative position between two lines
        /// </summary>
        /// <param name="intercept1">The first intercept</param>
        /// <param name="intercept2">The second intercept</param>
        /// <param name="slope1">The first rate of change</param>
        /// <param name="slope2">The second rate of change</param>
        /// <param name="x">The x value</param>
        /// <param name="y">The y value</param>
        /// <returns>The relative position</returns>
        private double GetRelativePositionBetweenLines(double intercept1, double intercept2, double slope1, double slope2, double x, double y)
        {
            double y1 = slope1 * x + intercept1;
            double y2 = slope2 * x + intercept2;

            return (y - y2) / Math.Abs(y1 - y2);
        }

        /// <summary>
        /// Given a timestamp which represents a missing value, interpolates the dataset using the first known point before the given point, and the first known point after the given point in the list of keys.
        /// </summary>
        /// <param name="valuesToInterpolate">A list of data point 'keys' where values are missing.</param>
        /// <param name="ds">A dataset to use, which indicates the length of time that elapses between readings.</param>
        /// <returns>A sensor state with the interpolated data.</returns>
        public SensorState Interpolate(List<DateTime> valuesToInterpolate, Dataset ds, ChangeReason reason)
        {
            EventLogger.LogInfo(_owner.Owner, GetType().ToString(), "Starting extrapolation process");

            if (valuesToInterpolate == null)
                throw new ArgumentNullException("valuesToInterpolate");

            if (valuesToInterpolate.Count == 0)
                throw new ArgumentException("You must specify at least one value to use for extrapolation.");

            if (ds == null)
                throw new ArgumentNullException("ds");

            //First remove values
            var newState = RemoveValues(valuesToInterpolate, reason);
            newState.Reason = reason;

            foreach (var time in valuesToInterpolate)
            {
                if (newState.Values.ContainsKey(time))
                    continue;

                DateTime startValue;
                try
                {
                    startValue = newState.FindPrevValue(time);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to find start value continuing");
                    continue;
                }

                DateTime endValue;
                try
                {
                    endValue = newState.FindNextValue(time);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to find end value continuing");
                    continue;
                }

                var timeDiff = endValue.Subtract(startValue).TotalMinutes;
                var valDiff = newState.Values[endValue] - newState.Values[startValue];
                var step = valDiff / (timeDiff / ds.DataInterval);
                var value = newState.Values[startValue] + step;

                for (var i = ds.DataInterval; i < timeDiff; i += ds.DataInterval)
                {
                    newState.Values[startValue.AddMinutes(i)] = (float)Math.Round(value, 2);

                    newState.AddToChanges(startValue.AddMinutes(i), reason.ID);

                    value += step;
                }
            }

            /* ==OLD METHOD==
                var first = valuesToInterpolate[0];
                DateTime startValue;
                try
                {
	                startValue = FindPrevValue(first, ds);
                }
                catch(Exception e)
                {
	                throw new DataException("No start value");
                }
                var endValue = DateTime.MinValue;
                var time = 0;
                try
                {
	                while (endValue == DateTime.MinValue)
	                {
		                endValue = (Values.ContainsKey(first.AddMinutes(time))
						                ? first.AddMinutes(time)
						                : DateTime.MinValue);
		                time += ds.DataInterval;
	                }
                }
                catch(Exception e)
                {
	                throw new DataException("No end value");
                }

                var timeDiff = endValue.Subtract(startValue).TotalMinutes;
                var valDiff = Values[endValue] - Values[startValue];
                var step = valDiff / (timeDiff / ds.DataInterval);
                var value = Values[startValue] + step;

                var newState = Clone();

                for (var i = ds.DataInterval; i < timeDiff; i += ds.DataInterval)
                {
	                newState.Values.Add(startValue.AddMinutes(i), (float)Math.Round(value, 2));
	                value += step;
                }

                EventLogger.LogInfo(GetType().ToString(), "Completing extrapolation process");*/

            return newState;
        }

        /// <summary>
        /// Given a timestamp as a key, finds the first known data point before the point represented by this key.
        /// </summary>
        /// <param name="dataValue">The key, representing an unknown data point, to use.</param>
        /// <returns>A key representing the first known data value before the value represented by dataValue.</returns>
        public DateTime FindPrevValue(DateTime dataValue)
        {
            return Values.Keys.Where(x => x < dataValue).Max();
        }

        /// <summary>
        /// Finds timestamp for the value coming after the given timestamp
        /// </summary>
        /// <param name="dataValue">The timestamp to look after</param>
        /// <returns>The next timestamp</returns>
        public DateTime FindNextValue(DateTime dataValue)
        {
            return Values.Keys.Where(x => x > dataValue).Min();
        }

        /// <summary>
        /// Returns a new sensor state where the given values are set to zero
        /// </summary>
        /// <param name="values">The values to set to zero</param>
        /// <param name="reason">The reason to make them zero</param>
        /// <returns>The new sensor state</returns>
        public SensorState MakeZero(List<DateTime> values, ChangeReason reason)
        {
            return MakeValue(values, 0, reason);
        }

        /// <summary>
        /// Returns a new sensor state where the given values are set to a specific value
        /// </summary>
        /// <param name="values">The values to set</param>
        /// <param name="value">The value to set to</param>
        /// <param name="reason">The reson for the change</param>
        /// <returns>The new sensor state</returns>
        public SensorState MakeValue(List<DateTime> values, float value, ChangeReason reason)
        {
            if (values == null)
                throw new ArgumentNullException("A non-null list of keys to set as " + value + " must be specified.");

            var newState = Clone();
            newState.Reason = reason;

            foreach (var time in values)
            {
                newState.Values[time] = value;
                newState.AddToChanges(time, reason.ID);
            }

            return newState;
        }

        public SensorState ChangeToZero(List<DateTime> values, ChangeReason reason)
        {
            return ChangeToValue(values, 0, reason);
        }

        public SensorState ChangeToValue(List<DateTime> values, float value, ChangeReason reason)
        {
            if (values == null)
                throw new ArgumentNullException("A non-null list of keys to set as " + value + " must be specified.");

            var newState = Clone();
            newState.Reason = reason;

            foreach (var time in values)
            {
                newState.Values[time] = value;
                newState.AddToChanges(time, reason.ID);
            }

            return newState;
        }

        /// <summary>
        /// Returns a new sensor state where the given values are removed
        /// </summary>
        /// <param name="values">The values to remove</param>
        /// <param name="reason">The reason for the change</param>
        /// <returns>The new sensor state</returns>
        public SensorState RemoveValues(List<DateTime> values, ChangeReason reason)
        {
            if (values == null)
                throw new ArgumentException("A non-null list to be removed must be specified");

            var newState = Clone();
            newState.Reason = reason;

            foreach (var time in values)
            {
                newState.Values.Remove(time);
                newState.AddToChanges(time, reason.ID);
            }

            return newState;
        }

        public override string ToString()
        {
            var title = (Values.Count > 0) ? Values.First().Key + " " + Values.First().Value : "Unknown";
            return _editTimestamp.ToString() + " " + title;
        }

        /// <summary>
        /// Logs a change
        /// </summary>
        /// <param name="sensorName">The name of the sensor</param>
        /// <param name="taskPerformed">The log to make</param>
        /// <returns>The log result</returns>
        public string LogChange(string sensorName, string taskPerformed)
        {
            return _owner != null ? EventLogger.LogSensorInfo(_owner.Owner, sensorName, taskPerformed + " Reason: " + Reason) : EventLogger.LogSensorInfo(null, sensorName, taskPerformed + " Reason: " + Reason);
        }

        /// <summary>
        /// The Upperline calculated
        /// </summary>
        public Dictionary<DateTime, float> UpperLine
        {
            get { return _upperLine; }
        }

        /// <summary>
        /// The Lowerline calculated
        /// </summary>
        public Dictionary<DateTime, float> LowerLine
        {
            get { return _lowerLine; }
        }

        /// <summary>
        /// Adds a change to the list of changes
        /// </summary>
        /// <param name="timestamp">The timestamp to add the change to</param>
        /// <param name="changeID">The change ID to add</param>
        public void AddToChanges(DateTime timestamp, int changeID)
        {
            if (!Changes.Keys.Contains(timestamp))
                Changes[timestamp] = new LinkedList<int>();

            Changes[timestamp].AddFirst(changeID);
        }
    }

    [ProtoContract]
    public class DataBlock
    {
        [ProtoMember(1)]
        public DateTime StartTime;
        [ProtoMember(2)]
        public int DataInterval;
        [ProtoMember(3, IsPacked = true)]
        public float[] Values;

        public override string ToString()
        {
            return Values.Aggregate(string.Format("Start Time {0} Data Interval {1} Values:\n\rCount {2}", StartTime, DataInterval,
                                 Values.Length), (current, value) => current + string.Format("\n\r[{0}]", value));
        }
    }
}
