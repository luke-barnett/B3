using System;

namespace IndiaTango.Models
{
    public class Event
    {
        //The time that an event occured
        //The action that was performed
        private string _action;

        /// <summary>
        /// Constructor that initialises the timeStamp and Action of the event
        /// </summary>
        /// <param name="timeStamp">The non-null time that the event occured</param>
        /// <param name="action">The non-null action that was performed at this event</param>
        public Event(DateTime timeStamp, string action)
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("The action must not be empty");
            TimeStamp = timeStamp;
            _action = action;
        }

        /// <summary>
        /// Read-Write non-null property for the time the event occured
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Read-Write non-null property for the action that was performed at this event
        /// </summary>
        public string Action
        {
            get { return _action; }
            set
            {
                if(string.IsNullOrEmpty(value))
                    throw new FormatException("The action must not be empty");
                _action = value;
            }
        }
    }
}