using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ProtoBuf;

namespace IndiaTango.Models
{
    public enum SummaryType { Average = 0, Sum = 1 }
    /// <summary>
    /// Represents a Sensor, which resembles a sensor attached to a buoy, measuring a given water quality parameter.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class Sensor : INotifyPropertyChanged
    {
        #region Private Members
        private Stack<SensorState> _undoStack;
        private Stack<SensorState> _redoStack;
        private List<Calibration> _calibrations;
        private string _name;
        private string _description;
        private float _depth;
        private string _unit;
        private float _maxRateOfChange;
        private int _errorThreshold;
        private Colour _colour;
        private float _lowerLimit;
        private float _upperLimit;
        private SummaryType _summaryType;
        [ProtoMember(16)]
        private SensorState _rawData;
        [ProtoMember(12)]
        private SensorState _currentState;
        [NonSerialized]
        private SensorVariable _sensorVariable;
        private string _sensorType;
        private ObservableCollection<SensorMetaData> _metaData;
        private SensorMetaData _currentMetaData;
        #endregion

        #region Constructors

        public Sensor() //For Protobuf-net
        {
            UndoStack = new Stack<SensorState>();
            RedoStack = new Stack<SensorState>();
            Calibrations = new List<Calibration>();
            _metaData = new ObservableCollection<SensorMetaData>();
            CurrentMetaData = new SensorMetaData("");
        }

        /// <summary>
        /// Creates a new sensor, with the specified sensor name and measurement unit.
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        public Sensor(string name, string unit) : this(name, "", 100, 0, unit, 0, null) { }

        /// <summary>
        /// Creates a new sensor, with the specified sensor name and measurement unit.
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        /// <param name="owner">The owner of the sensor</param>
        public Sensor(string name, string unit, Dataset owner) : this(name, "", 100, 0, unit, 0, owner) { }

        /// <summary>
        /// Creates a new sensor, using default values for Undo/Redo stacks, calibration dates, error threshold and a failure-indicating value.
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="description">A description of the sensor's function or purpose.</param>
        /// <param name="upperLimit">The upper limit for values reported by this sensor.</param>
        /// <param name="lowerLimit">The lower limit for values reported by this sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        /// <param name="maxRateOfChange">The maximum rate of change allowed by this sensor.</param>
        /// <param name="manufacturer">The manufacturer of this sensor.</param>
        /// <param name="serial">The serial number associated with this sensor.</param>
        /// <param name="owner">The dataset owner of the sensor</param>
        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, Dataset owner) : this(name, description, upperLimit, lowerLimit, unit, maxRateOfChange, new Stack<SensorState>(), new Stack<SensorState>(), new List<Calibration>(), owner) { }

        /// <summary>
        /// Creates a new sensor, using default values for error threshold and failure-indicating value. 
        /// </summary>
        /// <param name="name">The name of the sensor.</param>
        /// <param name="description">A description of the sensor's function or purpose.</param>
        /// <param name="upperLimit">The upper limit for values reported by this sensor.</param>
        /// <param name="lowerLimit">The lower limit for values reported by this sensor.</param>
        /// <param name="unit">The unit used to report values given by this sensor.</param>
        /// <param name="maxRateOfChange">The maximum rate of change allowed by this sensor.</param>
        /// <param name="manufacturer">The manufacturer of this sensor.</param>
        /// <param name="serial">The serial number associated with this sensor.</param>
        /// <param name="undoStack">A stack containing previous sensor states.</param>
        /// <param name="redoStack">A stack containing sensor states created after the modifications of the current state.</param>
        /// <param name="Calibrations">A list of dates, on which calibration was performed.</param>
        /// <param name="owner">The dataset owner of the sensor</param>
        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, Stack<SensorState> undoStack, Stack<SensorState> redoStack, List<Calibration> calibrations, Dataset owner) : this(name, description, upperLimit, lowerLimit, unit, maxRateOfChange, undoStack, redoStack, calibrations, Properties.Settings.Default.DefaultErrorThreshold, owner) { }

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
        /// <param name="serial">The serial number associated with this sensor.</param>
        /// <param name="undoStack">A stack containing previous sensor states.</param>
        /// <param name="redoStack">A stack containing sensor states created after the modifications of the current state.</param>
        /// <param name="calibrations">A list of dates, on which calibration was performed.</param>
        /// <param name="errorThreshold">The number of times a failure-indicating value can occur before this sensor is flagged as failing.</param>
        /// <param name="owner">The dataset that owns the sensor</param>
        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, Stack<SensorState> undoStack, Stack<SensorState> redoStack, List<Calibration> calibrations, int errorThreshold, Dataset owner) : this(name, description, upperLimit, lowerLimit, unit, maxRateOfChange, undoStack, redoStack, calibrations, errorThreshold, owner, SummaryType.Average) { }

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
        /// <param name="serial">The serial number associated with this sensor.</param>
        /// <param name="undoStack">A stack containing previous sensor states.</param>
        /// <param name="redoStack">A stack containing sensor states created after the modifications of the current state.</param>
        /// <param name="calibrations">A list of dates, on which calibration was performed.</param>
        /// <param name="errorThreshold">The number of times a failure-indicating value can occur before this sensor is flagged as failing.</param>
        /// <param name="owner">The dataset that owns the sensor</param>
        /// <param name="sType">Indicates whether the sensor's values should be averaged or summed when summarised</param>
        public Sensor(string name, string description, float upperLimit, float lowerLimit, string unit, float maxRateOfChange, Stack<SensorState> undoStack, Stack<SensorState> redoStack, List<Calibration> calibrations, int errorThreshold, Dataset owner, SummaryType sType)
        {
            if (name == "")
                throw new ArgumentNullException("Name");

            if (unit == "")
                throw new ArgumentNullException("Unit");

            if (calibrations == null)
                throw new ArgumentNullException("Calibrations");

            if (undoStack == null)
                throw new ArgumentNullException("UndoStack");

            if (redoStack == null)
                throw new ArgumentNullException("RedoStack");

            if (upperLimit <= lowerLimit)
                throw new ArgumentOutOfRangeException("UpperLimit");

            _name = name;
            RawName = name;
            Description = description;
            UpperLimit = upperLimit;
            LowerLimit = lowerLimit;
            _unit = unit;
            MaxRateOfChange = maxRateOfChange;
            _undoStack = undoStack;
            _redoStack = redoStack;
            _calibrations = calibrations;

            ErrorThreshold = errorThreshold;
            Owner = owner;
            _summaryType = sType;

            _metaData = new ObservableCollection<SensorMetaData>();
            CurrentMetaData = new SensorMetaData("");

            Colour = Color.FromRgb((byte)(Common.Generator.Next()), (byte)(Common.Generator.Next()), (byte)(Common.Generator.Next()));
        }

        #endregion

        #region Properties
        public ReadOnlyCollection<SensorState> UndoStates
        {
            get
            {
                // Return a stack that cannot be modified externally
                // Since it going to be iterated over in order anyway (and there'll only be approx. 5 times at any one time)...
                return _undoStack.ToList().AsReadOnly();
            }
        }

        public ReadOnlyCollection<SensorState> RedoStates
        {
            get
            {
                // Return a stack that cannot be modified externally
                // Since it going to be iterated over in order anyway (and there'll only be approx. 5 times at any one time)...
                return _redoStack.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the undo stack for this sensor. The undo stack contains a list of previous states this sensor was in before its current state. This stack cannot be null.
        /// </summary>
        private Stack<SensorState> UndoStack
        {
            get { return _undoStack; }
            set
            {
                if (value == null)
                    throw new FormatException("The undo stack cannot be null.");

                _undoStack = value;
            }
        }

        /// <summary>
        /// Gets the redo stack for this sensor. The redo stack contains a list of previous states this sensor can be in after the current state. This stack cannot be null.
        /// </summary>
        private Stack<SensorState> RedoStack
        {
            get { return _redoStack; }
            set
            {
                if (value == null)
                    throw new FormatException("The redo stack cannot be null.");

                _redoStack = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of dates this sensor was calibrated on. The list of calibration dates cannot be null.
        /// </summary>
        [ProtoMember(1)]
        public List<Calibration> Calibrations
        {
            get { return _calibrations; }
            set
            {
                if (value == null)
                    throw new FormatException("The list of calibration dates cannot be null.");

                _calibrations = value;
            }
        }

        /// <summary>
        /// Gets the first name this sensor was given (i.e. the name used to import)
        /// </summary>
        [ProtoMember(2)]
        public string RawName { get; private set; }

        /// <summary>
        /// Gets or sets the name of this sensor. The sensor name cannot be null.
        /// </summary>
        [ProtoMember(3)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != "")
                    _name = value;
                FirePropertyChanged("Name");
            }
        }

        /// <summary>
        /// Gets or sets the description of this sensor's purpose or function.
        /// </summary>
        [ProtoMember(4)]
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                FirePropertyChanged("Description");
            }
        }

        /// <summary>
        /// Gets or sets the depth of the sensor
        /// </summary>
        [ProtoMember(5)]
        public float Depth
        {
            get { return _depth; }
            set
            {
                _depth = value;
                FirePropertyChanged("Depth");
            }
        }

        /// <summary>
        /// Gets or sets the lower limit for values reported by this sensor.
        /// </summary>
        [ProtoMember(6)]
        public float LowerLimit
        {
            get { return _lowerLimit; }
            set
            {
                if (value > UpperLimit)
                {
                    _lowerLimit = UpperLimit;
                    UpperLimit = value;
                    EventLogger.LogSensorInfo(Owner, Name, "Swapping Upper and Lower Limit as Lower Limit > Upper Limit");
                }
                else
                    _lowerLimit = value;

                FirePropertyChanged("LowerLimit");
            }
        }

        /// <summary>
        /// Gets or sets the upper limit for values reported by this sensor.
        /// </summary>
        [ProtoMember(7)]
        public float UpperLimit
        {
            get { return _upperLimit; }
            set
            {
                if (value < LowerLimit)
                {
                    _upperLimit = LowerLimit;
                    LowerLimit = value;
                    EventLogger.LogSensorInfo(Owner, Name, "Swapping Upper and Lower Limit as Upper Limit < Lower Limit");
                }
                else
                    _upperLimit = value;
                FirePropertyChanged("UpperLimit");
            }
        }

        /// <summary>
        /// Gets or sets the measurement unit reported with values collected by this sensor.
        /// </summary>
        [ProtoMember(8)]
        public string Unit
        {
            get { return _unit; }
            set
            {
                if (value != "")
                    _unit = value;
                FirePropertyChanged("Unit");
            }
        }

        /// <summary>
        /// Gets or sets the maximum rate of change allowed for values reported by this sensor.
        /// </summary>
        [ProtoMember(9)]
        public float MaxRateOfChange
        {
            get
            {
                return _maxRateOfChange;
            }
            set
            {
                _maxRateOfChange = value;
                FirePropertyChanged("MaxRateOfChange");
            }
        }

        /// <summary>
        /// The variable used for calibration or equations
        /// </summary>
        public SensorVariable Variable
        {
            get { return _sensorVariable; }
            set
            {
                _sensorVariable = value;
                FirePropertyChanged("Variable");
            }
        }

        public SensorState CurrentState
        {
            get { return (_currentState == null) ? RawData : _currentState; }
            set { _currentState = value; }
        }

        /// <summary>
        /// The colour to use when graphing the sensor
        /// </summary>
        [ProtoMember(13)]
        public Colour Colour
        {
            get { return _colour; }
            set
            {
                _colour = value;
                FirePropertyChanged("Colour");
            }
        }

        [ProtoMember(14)]
        public int ErrorThreshold
        {
            get { return _errorThreshold; }
            set
            {
                if (value < 1)
                    throw new ArgumentException("Error threshold for any given sensor must be at least 1.");

                _errorThreshold = value;
            }
        }

        [ProtoMember(15, AsReference = true)]
        public Dataset Owner { get; private set; }

        public SensorState RawData
        {
            get { return _rawData ?? (_rawData = new SensorState(this, DateTime.Now, new Dictionary<DateTime, float>(), null, true, null)); }
            private set { _rawData = value; }
        }

        /// <summary>
        /// Gets a value indicating whether or not this sensor shows signs of physical failure.
        /// </summary>
        /// <param name="dataset">The dataset, containing information about the data interval for this sensor.</param>
        /// <returns>A value indicating whether or not this sensor is failing.</returns>
        public bool IsFailing(Dataset dataset)
        {
            if (Properties.Settings.Default.IgnoreSensorErrorDetection)
                return false;

            if (dataset == null)
                throw new NullReferenceException("You must provide a non-null dataset.");

            if (CurrentState == null)
                throw new NullReferenceException("No active sensor state exists for this sensor, so you can't detect whether it is failing or not.");

            if (CurrentState.Values.Count == 0)
                return false;

            var baseTime = CurrentState.Values.ElementAt(0).Key;
            var incidence = 0;
            var time = 0;

            for (int i = 0; i < dataset.ExpectedDataPointCount; i++)
            {
                var key = baseTime.AddMinutes(time);

                if (CurrentState.Values.ContainsKey(key))
                    incidence = 0;
                else
                    incidence++;

                if (incidence == ErrorThreshold)
                    return true;

                time += dataset.DataInterval;
            }

            return false;
        }

        [ProtoMember(17)]
        public SummaryType SummaryType
        {
            get { return _summaryType; }
            set
            {
                _summaryType = value;
                FirePropertyChanged("SummaryType");
            }
        }

        [ProtoMember(18)]
        public string SensorType
        {
            get { return _sensorType; }
            set
            {
                _sensorType = value;
                FirePropertyChanged("SensorType");
            }
        }

        public SensorMetaData CurrentMetaData
        {
            get { return _currentMetaData; }
            set
            {
                _currentMetaData = value;
                FirePropertyChanged("CurrentMetaData");
            }
        }

        [ProtoMember(22)]
        public ObservableCollection<SensorMetaData> MetaData
        {
            get { return _metaData; }
            set
            {
                _metaData = value;
                FirePropertyChanged("MetaData");
                CurrentMetaData = MetaData.OrderByDescending(x => x.DateOfInstallation).FirstOrDefault();
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Removes all undo states
        /// </summary>
        public void ClearUndoStates(bool alsoClearRedo = true)
        {
            UndoStack = new Stack<SensorState>();
            if (alsoClearRedo)
                RedoStack = new Stack<SensorState>();
        }
        /// <summary>
        /// Revert data values for this sensor to the previous state held.
        /// </summary>
        public void Undo()
        {
            // This is because the undo stack has to have at least one item on it - the current state
            if (UndoStack.Count == 0)
                throw new InvalidOperationException("Undo is not possible at this stage. There are no more possible states to undo to.");

            RedoStack.Push(CurrentState);
            _currentState = UndoStack.Pop();
        }

        /// <summary>
        /// Advance data values for this sensor to the next state stored.
        /// </summary>
        public void Redo()
        {
            if (RedoStack.Count == 0)
                throw new InvalidOperationException("Redo is not possible at this stage. There are no more possible states to redo to.");

            UndoStack.Push(CurrentState);
            _currentState = RedoStack.Pop();
        }

        /// <summary>
        /// Update the active state for data values, clearing the redo stack in the process.
        /// </summary>
        /// <param name="newState"></param>
        public void AddState(SensorState newState)
        {
            if (_rawData == null)
            {
                RawData = newState;
            }
            else
            {
                UndoStack.Push(CurrentState);
                RedoStack.Clear();

                _currentState = newState;
            }

        }

        public void RevertToRaw()
        {
            _currentState = null;
        }

        public void RevertToRaw(DateTime startDateTime, DateTime endDateTime, float lowerYLimit = float.NaN, float upperYLimit = float.NaN)
        {
            if (startDateTime > endDateTime)
                throw new ArgumentException("startDateTime");

            var newState = CurrentState.Clone();
            var changesMadeInTimePeriod = newState.Changes.Keys.Where(x => x >= startDateTime && x <= endDateTime).ToArray();
            if (!(float.IsNaN(lowerYLimit) || float.IsNaN(upperYLimit)))
            {
                changesMadeInTimePeriod =
                    changesMadeInTimePeriod.Where(
                        x => RawData.Values[x] >= lowerYLimit && RawData.Values[x] <= upperYLimit).ToArray();
            }
            foreach (var timeStamp in changesMadeInTimePeriod)
            {
                newState.Changes.Remove(timeStamp);
                newState.Values[timeStamp] = RawData.Values[timeStamp];
            }

            AddState(newState);
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Sensor))
                return false;

            if (this == obj)
                return true;

            var sensorObj = obj as Sensor;

            if (sensorObj.Name != Name)
                return false;

            //if (sensorObj.Owner != Owner) //TODO: Decide on a better way to handle this
            //    return false;

            //if (!sensorObj.Calibrations.Equals(Calibrations))
            //    return false;

            if (!sensorObj.CurrentState.Equals(CurrentState))
                return false;

            if (sensorObj.Depth != Depth)
                return false;

            if (sensorObj.Description != Description)
                return false;

            if (sensorObj.ErrorThreshold != ErrorThreshold)
                return false;

            if (sensorObj.LowerLimit != LowerLimit)
                return false;

            if (sensorObj.UpperLimit != UpperLimit)
                return false;

            if (sensorObj.MaxRateOfChange != MaxRateOfChange)
                return false;

            if (!sensorObj.RawData.Equals(RawData))
                return false;

            if (sensorObj.RawName != RawName)
                return false;

            if (sensorObj.SummaryType != SummaryType)
                return false;

            return ((sensorObj.Unit == null && Unit == null) || sensorObj.Unit.CompareTo(Unit) == 0);
        }

        #endregion

        /// <summary>
        /// Undoes this sensor's values so that the state represented by the timestamp specified is the current state (i.e. top of the Undo stack). If the timestamp does not exist, does nothing.
        /// </summary>
        /// <param name="dateTime">The timestamp representing the state to make the current state.</param>
        public void Undo(DateTime dateTime)
        {
            var sensor = (from selectedSensor in UndoStack where selectedSensor.EditTimestamp == dateTime select selectedSensor).DefaultIfEmpty(null).FirstOrDefault();

            if (sensor != null)
            {
                // Exists
                while (UndoStack.Count > 0)
                {
                    // Keep undoing until at desired state
                    if (CurrentState != sensor)
                        Undo();
                    else
                        break;
                }
            }
        }

        /// <summary>
        /// Advances this sensor's values so that the state represented by the timestamp specified is the current state (i.e. top of the Undo stack). If the timestamp does not exist, does nothing.
        /// </summary>
        /// <param name="dateTime">The timestamp representing the state to make the current state.</param>
        public void Redo(DateTime dateTime)
        {
            var sensor = (from selectedSensor in RedoStack where selectedSensor.EditTimestamp == dateTime select selectedSensor).DefaultIfEmpty(null).FirstOrDefault();

            if (sensor != null)
            {
                Redo();

                // Exists
                while (RedoStack.Count > 0)
                {
                    // Keep undoing until at desired state
                    if (CurrentState != sensor)
                        Redo();
                    else
                        break;
                }
            }
        }

        public string GuessConventionalNameForSensor()
        {
            // We will guess the name of the sensor, following convention,
            // by getting the first two words (even if each word is only a single
            // capital letter, and return them
            // The experts can then specify the depth
            var r = new Regex("(([A-Z])[a-z]*)");

            var mc = r.Matches(Name);

            if (mc.Count > 0)
            {
                if (mc.Count == 1)
                    return mc[0].Captures[0].Value;
                return mc[0].Captures[0].Value + mc[1].Captures[0].Value;
            }

            return Name;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string propertyThatChanged)
        {
            if (PropertyChanged != null && !string.IsNullOrWhiteSpace(propertyThatChanged))
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyThatChanged));
            }
        }

        public List<string> SensorVocabulary
        {
            get { return Models.SensorVocabulary.Vocabulary; }
        }

        public ObservableCollection<string> Units
        {
            get { return UnitsHelper.Units; }
        }
    }
}
