using System;

namespace IndiaTango.Models
{
    public class DataValue
    {
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }

        /// <summary>
        /// Creates a new DataValue class with the given time stamp and value
        /// </summary>
        /// <param name="timeStamp">The time stamp to use</param>
        /// <param name="value">The value to use</param>
        public DataValue(DateTime timeStamp, float value)
        {
            Timestamp = timeStamp;
            Value = value;
        }

        /// <summary>
        /// Checks Equality between this object and any given other
        /// </summary>
        /// <param name="obj">The object to check it against</param>
        /// <returns>The boolean result of the test</returns>
        public override bool Equals(object obj)
        {
            return (obj is DataValue) && (obj as DataValue).Timestamp == Timestamp && (obj as DataValue).Value.CompareTo(Value) == 0;
        }
    }
}
