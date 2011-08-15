using System;

namespace IndiaTango.Models
{
    /// <summary>
    /// Represents a data value recorded at a specific instance in time.
    /// </summary>
    public class DataValue
    {
        /// <summary>
        /// Gets or sets the date and time this data value was recorded at.
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Gets or sets the value this data value holds.
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Creates a new DataValue object with the specified date and time, and the specified value.
        /// </summary>
        /// <param name="timeStamp">The date and time this data value was recorded at.</param>
        /// <param name="value">The value recorded within this DataValue object.</param>
        public DataValue(DateTime timeStamp, float value)
        {
            Timestamp = timeStamp;
            Value = value;
        }

        /// <summary>
        /// Determines whether the given object is equal to this DataValue object.
        /// </summary>
        /// <param name="obj">The object to be compared.</param>
        /// <returns>Whether or not the given object is equal to this DataValue object.</returns>
        public override bool Equals(object obj)
        {
            return (obj is DataValue) && (obj as DataValue).Timestamp == Timestamp && (obj as DataValue).Value.CompareTo(Value) == 0;
        }
    }
}
