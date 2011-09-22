using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// A class representing a sensor state; that is, the values a sensor held at a given instance in time.
    /// </summary>
    public class SensorState
    {
        private DateTime _editTimestamp;
        private Dictionary<DateTime, float> _valueList;
        private string _reason = "";

        public SensorState Clone()
        {
            var d = _valueList.ToDictionary(v => v.Key, v => v.Value);
            var s = new SensorState(DateTime.Now,d);
            return s;
        }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        public SensorState(DateTime editTimestamp)
            : this(editTimestamp, new Dictionary<DateTime, float>())
        {
        }

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, and a list of values representing data values recorded in this state.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        public SensorState(DateTime editTimestamp, Dictionary<DateTime, float> valueList) : this(editTimestamp, valueList, "") {}

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, a list of values representing data values recorded in this state, and a reason for the changes stored in this state.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        /// <param name="reason">A string indicating the reason for the changes made in this state.</param>
        public SensorState(DateTime editTimestamp, Dictionary<DateTime, float> valueList, string reason)
        {
            if (valueList == null)
                throw new ArgumentNullException("The list of values in this state cannot be null.");

            _reason = reason;
            _editTimestamp = editTimestamp;
            _valueList = valueList;
        }

        /// <summary>
        /// Gets or sets the timestamp of the last edit to this sensor state.
        /// </summary>
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

        public List<DateTime> GetOutliersFromMaxAndMin(int timeGap,DateTime start, DateTime end,float upperLimit, float lowerLimit, float maxRateChange)
        {
            var outliers = new List<DateTime>();
            var prev = 0f;
            for(var time = start;time<=end;time = time.AddMinutes(timeGap))
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
        public List<DateTime> GetOutliersFromStdDev(int timeGap, DateTime start, DateTime end, int numStdDev, int smoothingPeriod)
        {
            var outliers = new List<DateTime>();
            for (var time = start; time <= end;time = time.AddMinutes(timeGap) )
            {
                var values = new List<float>();
                var value = 0f;
                if (!Values.TryGetValue(time, out value)) continue;
                for (var i = time; i < time.AddMinutes(timeGap * smoothingPeriod); i = i.AddMinutes(-timeGap))
                {
                    var avValue = 0f;
                    if (!Values.TryGetValue(time, out avValue)) continue;
                    values.Add(avValue);
                }
                var avg = values.Average();
                var sum = values.Sum(d => Math.Pow(d - avg, 2));
                var stdDev = (float)Math.Sqrt((sum) / (values.Count() - 1));
                var top = avg + (numStdDev*stdDev);
                var bottom = avg - (numStdDev*stdDev);
                if(value > top || value < bottom)
                {
                    outliers.Add(time);
                }
            }
            return outliers;
        }


        /// <summary>
        /// Given a timestamp which represents a missing value, extrapolates the dataset using the first known point before the given point, and the first known point after the given point in the list of keys.
        /// </summary>
        /// <param name="keys">A list of data point 'keys' where values are missing.</param>
        /// <param name="ds">A dataset to use, which indicates the length of time that elapses between readings.</param>
        /// <returns>A sensor state with the extrapolated data.</returns>
        public SensorState Extrapolate(List<DateTime> keys, Dataset ds)
        {
            EventLogger.LogInfo(GetType().ToString(), "Starting extrapolation process");

            if(keys == null)
                throw new ArgumentNullException("You must specify a list of keys.");

            if (keys.Count == 0)
                throw new ArgumentException("You must specify at least one value to use for extrapolation.");

            if (ds == null)
                throw new ArgumentNullException("You must specify the containing data set for this sensor.");

            var first = keys[0];
            var startValue = FindPrevValue(first, ds);
            var endValue = DateTime.MinValue;
            var time = 0;

            while (endValue == DateTime.MinValue)
            {
                endValue = (Values.ContainsKey(first.AddMinutes(time))
                                ? first.AddMinutes(time)
                                : DateTime.MinValue);
                time += ds.DataInterval;
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

            EventLogger.LogInfo(GetType().ToString(), "Completing extrapolation process");

            return newState;
        }

        /// <summary>
        /// Given a timestamp as a key, finds the first known data point before the point represented by this key.
        /// </summary>
        /// <param name="dataValue">The key, representing an unknown data point, to use.</param>
        /// <param name="ds">A dataset to use, primarily to determine the time that elapses between data points.</param>
        /// <returns>A key representing the first known data value before the value represented by dataValue.</returns>
        private DateTime FindPrevValue(DateTime dataValue, Dataset ds)
        {
            var prevValue = DateTime.MinValue;
            var time = 0;

            while (prevValue == DateTime.MinValue)
            {
                prevValue = (Values.ContainsKey(dataValue.AddMinutes(time))
                                 ? dataValue.AddMinutes(time)
                                 : DateTime.MinValue);
                time -= ds.DataInterval;
            }

            return prevValue;
        }

        public SensorState MakeZero(List<DateTime> values)
        {
            return MakeValue(values, 0);
        }

        public SensorState MakeValue(List<DateTime> values, float value)
        {
            if(values == null)
                throw new ArgumentNullException("A non-null list of keys to set as " + value + " must be specified.");

            var newState = Clone();

            foreach (var time in values)
            {
                newState.Values.Add(time, value);
            }

            return newState;
        }

        public SensorState ChangeToZero(List<DateTime> values )
        {
            return ChangeToValue(values, 0);
        }

        public SensorState ChangeToValue(List<DateTime> values, float value )
        {           
            if(values == null)
                throw new ArgumentNullException("A non-null list of keys to set as " + value + " must be specified.");

            var newState = Clone();

            foreach (var time in values)
            {
                newState.Values[time]= value;
            }

            return newState;
        }

        public SensorState removeValues(List<DateTime> values )
        {
            if(values == null)
                throw new ArgumentException("A non-null list to be removed must be specified");

            var newState = Clone();
            foreach (var time in values)
            {
                newState.Values.Remove(time);
            }

            return newState;
        }

        public override string ToString()
        {
            return _editTimestamp.ToString() + " " + Values.First().Key + " " + Values.First().Value;
        }
    }
}
