using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class SensorState
    {
        private DateTime _editTimestamp;
        private List<DataValue> _valueList;

        public SensorState(DateTime editTimestamp)
        {
            _editTimestamp = editTimestamp;
        }

        public SensorState(DateTime editTimestamp, List<DataValue> valueList)
        {
            _editTimestamp = editTimestamp;
            _valueList = valueList;
        }

        public DateTime EditTimestamp { get { return _editTimestamp; } set { _editTimestamp = value; } }

        public List<DataValue> Values { get { return _valueList; } set { _valueList = value; } }
    }
}
