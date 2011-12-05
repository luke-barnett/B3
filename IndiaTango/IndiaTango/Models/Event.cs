using System;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// Represents a pertinent event that can be recorded.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class Event
    {
        private string _action;

        private Event() { } // Necessary for serialisation

        /// <summary>
        /// Creates a new Event, specifying the date and time of the event, along with a string that identifies the action that occurred.
        /// </summary>
        /// <param name="timeStamp">The date and time this event occured. Cannot be null.</param>
        /// <param name="action">A string describing the action that occurred. Must not be empty.</param>
        public Event(DateTime timeStamp, string action)
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("The action must not be empty");

            TimeStamp = timeStamp;
            _action = action;
        }

        /// <summary>
        /// Gets or sets the date and time this event occurred.
        /// </summary>
        [ProtoMember(1)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets a string describing the action that occurred. 
        /// </summary>
        [ProtoMember(2)]
        public string Action
        {
            get { return _action; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new FormatException("The action must not be empty");
                _action = value;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Event && (obj as Event).Action == Action && (obj as Event).TimeStamp == TimeStamp;
        }
    }
}