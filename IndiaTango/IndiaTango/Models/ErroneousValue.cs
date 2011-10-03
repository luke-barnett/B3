using System;
using System.Collections.Generic;
using System.Linq;

namespace IndiaTango.Models
{
    public class ErroneousValue
    {
        private bool _hasValue;

        public ErroneousValue(DateTime timeStamp, float value, IDetectionMethod detector)
            : this(timeStamp, value)
        {
            Detectors.Add(detector);
        }

        public ErroneousValue(DateTime timeStamp, float value)
        {
            TimeStamp = timeStamp;
            Value = value;

            Detectors = new List<IDetectionMethod>();

            _hasValue = true;
        }

        public ErroneousValue(DateTime timeStamp, IDetectionMethod detector)
            : this(timeStamp)
        {
            Detectors.Add(detector);
        }

        public ErroneousValue(DateTime timeStamp)
        {
            TimeStamp = timeStamp;

            Detectors = new List<IDetectionMethod>();

            _hasValue = false;
        }

        public DateTime TimeStamp { get; set; }

        public float Value { get; set; }

        public List<IDetectionMethod> Detectors { get; private set; }

        public override string ToString()
        {
            var baseString = (_hasValue) ? string.Format("{0} {1}", TimeStamp, Value) : string.Format("{0}", TimeStamp);

            return Detectors.Aggregate(baseString, (current, detectionMethod) => current + string.Format(" [{0}]", detectionMethod.Name));
        }

        public override bool Equals(object obj)
        {
            return (obj != null) && obj.GetType() == GetType() && (obj as ErroneousValue).TimeStamp.CompareTo(TimeStamp) == 0;
        }
    }
}
