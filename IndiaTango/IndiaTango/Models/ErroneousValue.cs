using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace IndiaTango.Models
{
    /// <summary>
    /// Represents an erroneous value
    /// </summary>
    public class ErroneousValue
    {
        private readonly bool _hasValue;
        private readonly GraphCenteringCommand _command;
        private event GraphCenteringRequest RealEvent;
        private readonly ArrayList _delegates = new ArrayList();

        public ErroneousValue(DateTime timeStamp, float value, IDetectionMethod detector, Sensor owner)
            : this(timeStamp, value, owner)
        {
            Detectors.Add(detector);
        }

        public ErroneousValue(DateTime timeStamp, float value, Sensor owner)
            : this(timeStamp, owner)
        {
            Value = value;
            _hasValue = true;
        }

        public ErroneousValue(DateTime timeStamp, IDetectionMethod detector, Sensor owner)
            : this(timeStamp, owner)
        {
            Detectors.Add(detector);
        }

        public ErroneousValue(DateTime timeStamp, Sensor owner)
        {
            Owner = owner;

            TimeStamp = timeStamp;

            Detectors = new List<IDetectionMethod>();

            _hasValue = false;

            _command = new GraphCenteringCommand(this);
        }

        /// <summary>
        /// The timestamp of the value
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The value
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// The sensor the value belongs to
        /// </summary>
        public Sensor Owner { get; set; }

        /// <summary>
        /// The list of detectors that found the value
        /// </summary>
        public List<IDetectionMethod> Detectors { get; private set; }

        public event GraphCenteringRequest GraphCenteringRequested
        {
            add
            {
                RealEvent += value;
                _delegates.Add(value);
            }

            remove
            {
                RealEvent -= value;
                _delegates.Remove(value);
            }
        }

        /// <summary>
        /// The string representation of the erroneous value
        /// </summary>
        public string AsString
        {
            get { return ToString(); }
        }

        public ErroneousValue This
        {
            get { return this; }
        }

        public override string ToString()
        {
            var baseString = (Owner != null) ? string.Format("[{0}] ", Owner) : "";
            baseString += (_hasValue) ? string.Format("{0} {1}", TimeStamp, Value) : string.Format("{0}", TimeStamp);

            return Detectors.Aggregate(baseString, (current, detectionMethod) => current + string.Format(" [{0}]", detectionMethod.Name));
        }

        public override bool Equals(object obj)
        {
            return (obj != null) && obj.GetType() == GetType() && ((ErroneousValue)obj).TimeStamp.CompareTo(TimeStamp) == 0;
        }

        public void RequestGraphCentering()
        {
            if (RealEvent != null)
                RealEvent();
        }

        public void RemoveAllEvents()
        {
            foreach (GraphCenteringRequest eventHandler in _delegates)
            {
                RealEvent -= eventHandler;
            }
            _delegates.Clear();
        }

        public ICommand RequestGraphCenteringCommand
        {
            get { return _command; }
        }
    }

    public delegate void GraphCenteringRequest();

    public class GraphCenteringCommand : ICommand
    {
        private readonly ErroneousValue _erroneous;

        public GraphCenteringCommand(ErroneousValue erroneous)
        {
            _erroneous = erroneous;
        }

        public void Execute(object parameter)
        {
            _erroneous.RequestGraphCentering();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
