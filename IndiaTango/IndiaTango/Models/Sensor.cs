﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IndiaTango.Models
{
    public class Sensor
    {
        #region Private Members
        private Stack<SensorState> _undoStack;
        private Stack<SensorState> _redoStack;
        private List<DateTime> _calibrationDates;
        private string _name;
        private string _unit;
        #endregion

        #region Constructors
        public Sensor() { } // TODO: Just put here to satisfy some tests, but it's not ideal - remove later

        /// <summary>
        /// Creates a new sensor, with the specified sensor name and measurement unit.
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        public Sensor(string name, string unit) : this(name, "", 0, 0, unit, 0, "") { }

        /// <summary>
        /// Creates a new sensor.
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="description">A description of the sensor's function or purpose.</param>
        /// <param name="upperLimit">The upper limit for values reported by this sensor.</param>
        /// <param name="lowerLimit">The lower limit for values reported by this sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        /// <param name="maxRateOfChange">The maximum rate of change allowed by this sensor.</param>
        /// <param name="manufacturer">The manufacturer of this sensor.</param>
        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, string manufacturer) : this(name, description, upperLimit, lowerLimit, unit, maxRateOfChange, manufacturer, new Stack<SensorState>(), new Stack<SensorState>(), new List<DateTime>()) { }

        /// <summary>
        /// Creates a new sensor.
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="description">A description of the sensor's function or purpose.</param>
        /// <param name="upperLimit">The upper limit for values reported by this sensor.</param>
        /// <param name="lowerLimit">The lower limit for values reported by this sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        /// <param name="maxRateOfChange">The maximum rate of change allowed by this sensor.</param>
        /// <param name="manufacturer">The manufacturer of this sensor.</param>
        /// <param name="undoStack">A stack containing previous sensor states.</param>
        /// <param name="redoStack">A stack containing sensor states created after the modifications of the current state.</param>
        /// <param name="calibrationDates">A list of dates, on which calibration was performed.</param>
        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, string manufacturer, Stack<SensorState> undoStack, Stack<SensorState> redoStack, List<DateTime> calibrationDates)
        {
            if (name == "")
                throw new ArgumentNullException("Sensor name cannot be empty.");

            if (unit == "")
                throw new ArgumentNullException("Sensor Unit cannot be empty.");

            if (calibrationDates == null)
                throw new ArgumentNullException("The list of calibration dates cannot be null.");

            if (undoStack == null)
                throw new ArgumentNullException("The undo stack cannot be null");

            if (redoStack == null)
                throw new ArgumentNullException("The redo stack cannot be null.");

            _name = name;
            Description = description;
            UpperLimit = upperLimit;
            LowerLimit = lowerLimit;
            _unit = unit;
            MaxRateOfChange = maxRateOfChange;
            Manufacturer = manufacturer;
            _undoStack = undoStack;
            _redoStack = redoStack;
            _calibrationDates = calibrationDates;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the undo stack for this sensor. The undo stack contains a list of previous states this sensor was in before its current state. This stack cannot be null.
        /// </summary>
        public Stack<SensorState> UndoStack
        {
            get { return _undoStack; }
            set
            {
                if(value == null)
                    throw new FormatException("The undo stack cannot be null.");

                _undoStack = value;
            }
        }

        /// <summary>
        /// Gets or sets the redo stack for this sensor. The redo stack contains a list of previous states this sensor can be in after the current state. This stack cannot be null.
        /// </summary>
        public Stack<SensorState> RedoStack
        {
            get { return _redoStack; }
            set
            {
                if(value == null)
                    throw new FormatException("The redo stack cannot be null.");

                _redoStack = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of dates this sensor was calibrated on. The list of calibration dates cannot be null.
        /// </summary>
        public List<DateTime> CalibrationDates
        {
            get { return _calibrationDates; }
            set
            {
                if(value == null)
                    throw new FormatException("The list of calibration dates cannot be null.");

                _calibrationDates = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of this sensor. The sensor name cannot be null.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set 
            {
                if (value == "") throw new FormatException("Sensor name cannot be empty.");
                _name = value; 
            }
        }

        /// <summary>
        /// Gets or sets the description of this sensor's purpose or function.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the lower limit for values reported by this sensor.
        /// </summary>
        public float LowerLimit { get; set; }

        /// <summary>
        /// Gets or sets the upper limit for values reported by this sensor.
        /// </summary>
        public float UpperLimit { get; set; }

        /// <summary>
        /// Gets or sets the measurement unit reported with values collected by this sensor.
        /// </summary>
        public string Unit
        {
            get { return _unit; }
            set
            {
                if(value == "") throw new FormatException("Sensor Unit cannot be empty.");
                _unit = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum rate of change allowed for values reported by this sensor.
        /// </summary>
        public float MaxRateOfChange { get; set; }

        /// <summary>
        /// Gets or sets the name of the manufacturer of this sensor.
        /// </summary>
        public string Manufacturer { get; set; }
        #endregion

        #region Public Methods
        public void Undo()
        {
            RedoStack.Push(UndoStack.Pop());
        }
        #endregion
    }
}
