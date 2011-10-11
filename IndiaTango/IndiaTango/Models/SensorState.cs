using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace IndiaTango.Models
{
    /// <summary>
    /// A class representing a sensor state; that is, the values a sensor held at a given instance in time.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SensorState
    {
        private DateTime _editTimestamp;
        private Dictionary<DateTime, float> _valueList;
        private string _reason = "";
        private Dictionary<DateTime, float> _upperLine;
        private Dictionary<DateTime, float> _lowerLine;
        private Dictionary<DateTime, LinkedList<int>> _changes;
        private bool _isRaw = false;

        public SensorState Clone()
        {
            var d = _valueList.ToDictionary(v => v.Key, v => v.Value);
            var c = _changes.ToDictionary(v => v.Key, v => v.Value);
            var s = new SensorState(d,c);
            
            return s;
        }

        #region Constructors
        public SensorState(Dictionary<DateTime, float> valueList, string reason, Dictionary<DateTime, LinkedList<int>> changes ) : this(DateTime.Now, valueList, reason, false, changes) { }
        public SensorState(Dictionary<DateTime, float> valueList,Dictionary<DateTime,LinkedList<int>> changes) : this(DateTime.Now, valueList, changes) { }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        public SensorState(DateTime editTimestamp)
            : this(editTimestamp, new Dictionary<DateTime, float>(), new Dictionary<DateTime, LinkedList<int>>())
        {
        }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, and a list of values representing data values recorded in this state.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        public SensorState(DateTime editTimestamp, Dictionary<DateTime, float> valueList, Dictionary<DateTime,LinkedList<int>> changes) : this(editTimestamp, valueList, "", false, changes) { }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, a list of values representing data values recorded in this state, and a reason for the changes stored in this state.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        /// <param name="reason">A string indicating the reason for the changes made in this state.</param>
        /// <param name="isRaw">Whether or not this represents the sensors raw data.</param>
        public SensorState(DateTime editTimestamp, Dictionary<DateTime, float> valueList, string reason, bool isRaw, Dictionary<DateTime, LinkedList<int>> changes)
        {
            if (valueList == null)
                throw new ArgumentNullException("The list of values in this state cannot be null.");

            _reason = reason;
            _editTimestamp = editTimestamp;
            _valueList = valueList;
            _isRaw = isRaw;
            _changes = changes ?? new Dictionary<DateTime, LinkedList<int>>();
        }

#endregion

        [DataMember]
        public Dictionary<DateTime, LinkedList<int>> Changes
        {
            get { return _changes; }
            set { _changes = value; }
        }
        
        [DataMember]
        public bool IsRaw
        {
            get { return _isRaw; }
            private set { _isRaw = value; }
        }

        /// <summary>
        /// Gets or sets the timestamp of the last edit to this sensor state.
        /// </summary>
        [DataMember]
        public DateTime EditTimestamp
        {
            get { return _editTimestamp; }
            set { _editTimestamp = value; }
        }

        /// <summary>
        /// Gets or sets the list of values this sensor state holds.
        /// </summary>
        [DataMember]
        public Dictionary<DateTime, float> Values
        {
            get { return _valueList; }
            set { _valueList = value; }
        }

        public string Reason
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
            SensorState s = null;

            if (!(obj is SensorState))
                return false;

            s = obj as SensorState;

            if (!(s.EditTimestamp == EditTimestamp))
                return false;

            if (s.Values.Count != Values.Count)
                return false;

            foreach (var f in _valueList)
            {
                if (!s.Values[f.Key].Equals(f.Value))
                    return false;
            }

            return true;
        }

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

        public List<DateTime> GetOutliersFromMaxAndMin(int timeGap, DateTime start, DateTime end, float upperLimit, float lowerLimit, float maxRateChange)
        {
            var outliers = new List<DateTime>();
            _upperLine = new Dictionary<DateTime, float> { { start, upperLimit }, { end, upperLimit } };
            _lowerLine = new Dictionary<DateTime, float> { { start, lowerLimit }, { end, lowerLimit } };

            var prev = 0f;
            for (var time = start; time <= end; time = time.AddMinutes(timeGap))
            {
                var value = 0f;
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
                var value = 0f;
                value = (Values.TryGetValue(i, out value) ? value : float.NaN);
                values.AddLast(value);
            }
            for (var time = start; time <= end; time = time.AddMinutes(timeGap))
            {
                values.RemoveFirst();
                var next = 0f;
                next = (Values.TryGetValue(time.AddMinutes(timeGap * (smoothingPeriod / 2)), out next) ? next : float.NaN);
                values.AddLast(next);
                var value = 0f;
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

        private float GetSquaresSum(IEnumerable<float> values, float average)
        {
            return values.Where(value => !float.IsNaN(value)).Sum(v => (float)Math.Pow((v - average), 2));
        }

        private int GetCount(IEnumerable<float> values)
        {
            return values.Count(value => !float.IsNaN(value));
        }

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

		public SensorState Calibrate(DateTime start, DateTime end, double origA, double origB, double newA, double newB)
		{
			if(start >= end)
				throw new ArgumentException("End time must be greater than start time");

			if(origB > origA)
				throw new ArgumentException("Original B value must be less than original A value");

			if (newB > newA)
				throw new ArgumentException("New B value must be less than new A value");

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
			double slopeA = (newA - origA)/totalMinutes;
			double slopeMedian = ((newB + newMedianDistance) - (origB + origMedianDistance))/totalMinutes;

			//The y-intercepts of the lines
			double interceptA = origA;
			double interceptMedian = origB + origMedianDistance;

			//Copy!
			SensorState newState = Clone();

			foreach (var value in Values)
			{
				if(value.Key < start || value.Key > end)
					continue;

				double timeDiff = value.Key.Subtract(start).TotalMinutes;
				double normalisationScale = GetRelativePositionBetweenLines(interceptA, interceptMedian, slopeA, slopeMedian, timeDiff, value.Value);

				newState.Values[value.Key] = (float)(value.Value - (normalisationScale * normalisationIncrement + calibrationIncrement) * timeDiff);
			}

			return newState;
		}

		private double GetRelativePositionBetweenLines(double intercept1, double intercept2, double slope1, double slope2, double x, double y)
		{
			double y1 = slope1*x + intercept1;
			double y2 = slope2*x + intercept2;

			return (y - y2)/Math.Abs(y1 - y2);
		}


        /// <summary>
        /// Given a timestamp which represents a missing value, extrapolates the dataset using the first known point before the given point, and the first known point after the given point in the list of keys.
        /// </summary>
        /// <param name="valuesToExtrapolate">A list of data point 'keys' where values are missing.</param>
        /// <param name="ds">A dataset to use, which indicates the length of time that elapses between readings.</param>
        /// <returns>A sensor state with the extrapolated data.</returns>
        public SensorState Extrapolate(List<DateTime> valuesToExtrapolate, Dataset ds)
        {
            EventLogger.LogInfo(GetType().ToString(), "Starting extrapolation process");

            if (valuesToExtrapolate == null)
                throw new ArgumentNullException("You must specify a list of keys.");

            if (valuesToExtrapolate.Count == 0)
                throw new ArgumentException("You must specify at least one value to use for extrapolation.");

            if (ds == null)
                throw new ArgumentNullException("You must specify the containing data set for this sensor.");

            var newState = Clone();

            foreach (var time in valuesToExtrapolate)
            {
                DateTime startValue;
                try
                {
                    startValue = FindPrevValue(time);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to find start value continuing");
                    continue;
                }

                DateTime endValue;
                try
                {
                    endValue = FindNextValue(time);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to find end value continuing");
                    continue;
                }

                var timeDiffBetweenEndPoints = endValue.Subtract(startValue).TotalMinutes;
                var timeDiffBetweenStartAndPoint = time.Subtract(startValue).TotalMinutes;

                var valueDiff = Values[endValue] - Values[startValue];

                var newValue = (float)(valueDiff * (timeDiffBetweenStartAndPoint / timeDiffBetweenEndPoints)) + Values[startValue];

                newState.Values[time] = newValue;
                if (_changes.ContainsKey(time))
                    newState.Changes[time].AddFirst(EventLogger.NextRefNum);
                else
                {
                    newState.Changes.Add(time, new LinkedList<int>());
                    newState.Changes[time].AddFirst(EventLogger.NextRefNum);
                }
            }

            /* ==OLD METHOD==
                var first = valuesToExtrapolate[0];
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

        public DateTime FindNextValue(DateTime dataValue)
        {
            return Values.Keys.Where(x => x > dataValue).Min();
        }

        public SensorState MakeZero(List<DateTime> values)
        {
            return MakeValue(values, 0);
        }

        public SensorState MakeValue(List<DateTime> values, float value)
        {
            if (values == null)
                throw new ArgumentNullException("A non-null list of keys to set as " + value + " must be specified.");

            var newState = Clone();

            foreach (var time in values)
            {
                newState.Values[time] = value;
                AddChange(newState, time);
            }

            return newState;
        }

        public SensorState ChangeToZero(List<DateTime> values)
        {
            return ChangeToValue(values, 0);
        }

        public SensorState ChangeToValue(List<DateTime> values, float value)
        {
            if (values == null)
                throw new ArgumentNullException("A non-null list of keys to set as " + value + " must be specified.");

            var newState = Clone();

            foreach (var time in values)
            {
                newState.Values[time] = value;
                AddChange(newState, time);
            }

            return newState;
        }

        public static void AddChange(SensorState newState, DateTime time)
        {
            if (newState.Changes.ContainsKey(time))
                newState.Changes[time].AddFirst(EventLogger.NextRefNum);
            else
            {
                newState.Changes.Add(time, new LinkedList<int>());
                newState.Changes[time].AddFirst(EventLogger.NextRefNum);
            }
        }

        public SensorState RemoveValues(List<DateTime> values)
        {
            if (values == null)
                throw new ArgumentException("A non-null list to be removed must be specified");

            var newState = Clone();
            foreach (var time in values)
            {
                newState.Values.Remove(time);
                AddChange(newState, time);
            }

            return newState;
        }

        // TODO: test this!
        public override string ToString()
        {
            var title = (Values.Count > 0) ? Values.First().Key + " " + Values.First().Value : "Unknown";
            return _editTimestamp.ToString() + " " + title;
        }

        public string LogChange(string sensorName, string taskPerformed)
        {
            return EventLogger.LogSensorInfo(sensorName, taskPerformed + " Reason: " + Reason);
        }

        public Dictionary<DateTime, float> UpperLine
        {
            get { return _upperLine; }
        }

        public Dictionary<DateTime, float> LowerLine
        {
            get { return _lowerLine; }
        }

		private bool IsPointAbove(double x1, double y1, double x2, double y2, double xPoint, double yPoint)
		{
			return ((x2 - x1)*(yPoint - y1) - (y2 - y1)*(xPoint - x1)) > 0;
		}


    }
}
