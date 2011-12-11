using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class SensorStateListObject
    {
        private SensorState _object = null;
        private bool _isRaw = false;

        public SensorStateListObject(SensorState obj, bool raw)
        {
            _object = obj;
            _isRaw = raw;
        }

        public SensorState State
        {
            get { return _object; }
        }

        public bool IsRaw
        {
            get { return _isRaw; }
        }

        public override string ToString()
        {
            if (_isRaw)
                return "Revert to Raw Data";

            return ((_object != null) ? _object.EditTimestamp.ToString() : "Unknown Date") + " - " + (_object != null ? _object.Reason.ToString() : "");
        }
    }
}
