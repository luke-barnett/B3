using System;

namespace IndiaTango.Models
{
    /// <summary>
    /// Object to represent a formatted date time
    /// </summary>
    public class FormattedDateTime
    {
        public FormattedDateTime(DateTime time)
        {
            Time = time;
        }

        public DateTime Time { get; private set; }

        public override string ToString()
        {
            return Time.ToString("yyyy/MM/dd hh:mm");
        }
    }
}
