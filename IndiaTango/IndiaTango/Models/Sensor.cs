using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IndiaTango.Models
{
    public class Sensor
    {
        private Stack<SensorState> _undoStack;
        private Stack<SensorState> _redoStack;
        private List<DateTime> _calibrationDates;

        public Sensor(Stack<SensorState> undoStack, Stack<SensorState> redoStack) : this(undoStack, redoStack, new List<DateTime>()) { }

        public Sensor(Stack<SensorState> undoStack, Stack<SensorState> redoStack, List<DateTime> calibrationDates)
        {
            if (calibrationDates == null)
                throw new ArgumentNullException("The list of calibration dates cannot be null.");

            if (undoStack == null)
                throw new ArgumentNullException("The undo stack cannot be null");

            if (redoStack == null)
                throw new ArgumentNullException("The redo stack cannot be null.");

            this._undoStack = undoStack;
            this._redoStack = redoStack;
            this._calibrationDates = calibrationDates;
        }

        public Sensor() : this(new Stack<SensorState>(), new Stack<SensorState>(), new List<DateTime>()) { }

        public Stack<SensorState> UndoStack
        {
            get { return _undoStack; }
            set { _undoStack = value; } // TODO: test not set to null
        }

        public Stack<SensorState> RedoStack
        {
            get { return _redoStack; }
            set { _redoStack = value; } // TODO: test not set to null
        }

        public List<DateTime> CalibrationDates
        {
            get { return _calibrationDates; }
            set { _calibrationDates = value; }
        }
    }
}
