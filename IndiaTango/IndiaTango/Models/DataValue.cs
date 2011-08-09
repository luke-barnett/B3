using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class DataValue
    {
        public DateTime Timestamp;
        public float Value;

        public DataValue(DateTime timeStamp, float value)
        {
            Timestamp = timeStamp;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return (obj is DataValue) && (obj as DataValue).Timestamp == Timestamp && (obj as DataValue).Value == Value;
        }
    }
}
