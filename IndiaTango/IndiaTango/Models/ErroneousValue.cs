﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace IndiaTango.Models
{
    public class ErroneousValue
    {
        private bool _hasValue;

        private GraphCenteringCommand _command;

        private event GraphCenteringRequest _realEvent;

        private ArrayList _delegates = new ArrayList();

        public ErroneousValue(DateTime timeStamp, float value, IDetectionMethod detector)
            : this(timeStamp, value)
        {
            Detectors.Add(detector);
        }

        public ErroneousValue(DateTime timeStamp, float value)
            : this(timeStamp)
        {
            Value = value;
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

            _command = new GraphCenteringCommand(this);
        }

        public DateTime TimeStamp { get; set; }

        public float Value { get; set; }

        public List<IDetectionMethod> Detectors { get; private set; }

        public event GraphCenteringRequest GraphCenteringRequested
        {
            add
            {
                _realEvent += value;
                _delegates.Add(value);
            }

            remove
            {
                _realEvent -= value;
                _delegates.Remove(value);
            }
        }


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
            var baseString = (_hasValue) ? string.Format("{0} {1}", TimeStamp, Value) : string.Format("{0}", TimeStamp);

            return Detectors.Aggregate(baseString, (current, detectionMethod) => current + string.Format(" [{0}]", detectionMethod.Name));
        }

        public override bool Equals(object obj)
        {
            return (obj != null) && obj.GetType() == GetType() && (obj as ErroneousValue).TimeStamp.CompareTo(TimeStamp) == 0;
        }

        public void RequestGraphCentering()
        {
            if (_realEvent != null)
                _realEvent();
        }

        public void RemoveAllEvents()
        {
            foreach (GraphCenteringRequest eventHandler in _delegates)
            {
                _realEvent -= eventHandler;
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
