using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class DataValue
    {
        public DataValue(float value, DateTime timeStamp)
        {
            Value = value;
            TimeStamp = timeStamp;
        }

        public float Value { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
