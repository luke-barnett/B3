﻿using System;
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
        private List<DataValue> _valueList;

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        public SensorState(DateTime editTimestamp) : this(editTimestamp, new List<DataValue>()) {}

        /// <summary>
        /// Creates a new sensor state with the specified timestamp representing the date it was last edited, and a list of values representing data values recorded in this state.
        /// </summary>
        /// <param name="editTimestamp">A DateTime object representing the last edit date and time for this sensor state.</param>
        /// <param name="valueList">A list of data values, representing values recorded in this sensor state.</param>
        public SensorState(DateTime editTimestamp, List<DataValue> valueList)
        {
            if(valueList == null)
                throw new ArgumentNullException("The list of values in this state cannot be null.");

            _editTimestamp = editTimestamp;
            _valueList = valueList;
        }

        /// <summary>
        /// Gets or sets the timestamp of the last edit to this sensor state.
        /// </summary>
        public DateTime EditTimestamp { get { return _editTimestamp; } set { _editTimestamp = value; } }

        /// <summary>
        /// Gets or sets the list of values this sensor state holds.
        /// </summary>
        public List<DataValue> Values { get { return _valueList; } set { _valueList = value; } }

        /// <summary>
        /// Determines whether a given object is equal to this SensorState object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>Whether or not the given object, and this SensorState, are equal.</returns>
        public override bool Equals(object obj) // TODO: test this
        {
            SensorState s = null;

            if(!(obj is SensorState))
                return false;

            s = obj as SensorState;

            if(!(s.EditTimestamp == EditTimestamp))
                return false;

            if(s.Values.Count != Values.Count)
                return false;

            for (int i = 0; i < Values.Count; i++)
                if (!s.Values[i].Equals(Values[i]))
                    return false;

            return true;
        }



        public List<DataValue> GetMissingTimes(int timeDiff)
        {
            var missing = new List<DataValue>();
                for (int i = 1; i < Values.Count; i++)
                {
                    if (Values[i - 1].Timestamp.AddMinutes(timeDiff) != Values[i].Timestamp)
                    {
                        var tmpTime = Values[i - 1].Timestamp.AddMinutes(timeDiff);
                        while (tmpTime < Values[i].Timestamp)
                        {
                            missing.Add(new DataValue(tmpTime, 0));
                            tmpTime = tmpTime.AddMinutes(timeDiff);
                        }

                }
            }
            return missing;
        }

    }
}
