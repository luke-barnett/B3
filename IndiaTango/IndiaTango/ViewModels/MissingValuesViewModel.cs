using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    public class MissingValuesViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private Sensor _sensor;
        private List<DateTime> _missingValues = new List<DateTime>();
        private List<DateTime> _selectedValues = new List<DateTime>();
        private Dataset _ds;
        private Cursor _viewCursor = Cursors.Arrow;

        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private BehaviourManager _behaviour;
        private int _sampleRate;
        private GraphableSensor _graphableSensor;
        private Canvas _backgroundCanvas;

        private DoubleRange _range;
        private double _minimum;
        private double _minimumMinimum;
        private double _maximumMinimum;
        private double _maximum;
        private double _minimumMaximum;
        private double _maximumMaximum;
        private List<String> _samplingCaps = new List<string>();
        private int _samplingCapIndex;

        public MissingValuesViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;

            _backgroundCanvas = new Canvas { Visibility = Visibility.Collapsed };

            _behaviour = new BehaviourManager { AllowMultipleEnabled = true };

            var backgroundBehaviour = new GraphBackgroundBehaviour(_backgroundCanvas) { IsEnabled = true };

            _behaviour.Behaviours.Add(backgroundBehaviour);

            var zoomBehaviour = new CustomZoomBehaviour { IsEnabled = true };
            zoomBehaviour.ZoomRequested += (o, e) =>
                                               {
                                                   var startTime = e.LowerX;
                                                   var endTime = e.UpperX;
                                                   _graphableSensor.SetUpperAndLowerBounds(startTime, endTime);
                                                   UpdateGraph();
                                               };

            zoomBehaviour.ZoomResetRequested += o =>
                                                    {
                                                        _graphableSensor.RemoveBounds();
                                                        UpdateGraph();
                                                    };

            _behaviour.Behaviours.Add(zoomBehaviour);

            Behaviour = _behaviour;

            SamplingCaps = new List<string>(Common.GenerateSamplingCaps());
            SelectedSamplingCapIndex = 3;
        }

        #region View Properties

        private bool IsAtRaw = false;

        public Cursor ViewCursor
        {
            get { return _viewCursor; }
            set { _viewCursor = value; NotifyOfPropertyChange(() => ViewCursor); }
        }

        public bool RedoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.RedoStates.Count > 0; }
        }

        public bool UndoButtonEnabled
        {
            get { return SelectedSensor != null && !SelectedSensor.CurrentState.IsRaw; }
        }

        public Dataset Dataset { get { return _ds; } set { _ds = value; } }

        public String SensorName
        {
            get { return SelectedSensor == null ? "" : SelectedSensor.Name; }
        }

        public List<DateTime> MissingValues
        {
            get { return _missingValues; }
            set
            {
                _missingValues = value;
                NotifyOfPropertyChange(() => MissingValuesStrings);
            }
        }

        public List<string> MissingValuesStrings
        {
            get
            {
                var list = new List<string>();
                foreach (var value in _missingValues)
                {
                    list.Add(value.ToString());
                }
                return list;
            }
        }

        public int MissingCount
        {
            get { return _missingValues.Count; }
            set { var i = value; }
        }

        public List<Sensor> SensorList
        {
            get { return _ds.Sensors; }
            set
            {
                _ds.Sensors = value;
                NotifyOfPropertyChange(() => SensorList);
            }
        }

        public Sensor SelectedSensor
        {
            get { return _sensor; }
            set
            {
                _sensor = value;
                NotifyOfPropertyChange(() => SelectedSensor);

                if (_sensor != null && _sensor.CurrentState != null)
                    MissingValues = _sensor.CurrentState.GetMissingTimes(15, _ds.StartTimeStamp, _ds.EndTimeStamp);

                NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => MissingCount);
                NotifyOfPropertyChange(() => RedoButtonEnabled);
                NotifyOfPropertyChange(() => UndoButtonEnabled);
                _graphableSensor = _sensor != null ? new GraphableSensor(_sensor) : null;
                UpdateGraph();
            }
        }

        public List<DateTime> SelectedValues
        {
            get { return _selectedValues; }
            set
            {
                _selectedValues = value;
                NotifyOfPropertyChange(() => SelectedValues);
            }
        }

        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }

        public BehaviourManager Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        public string ChartTitle { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Name; } }

        public string YAxisTitle { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Unit; } }

        public DoubleRange Range
        {
            get { return _range; }
            set
            {
                _range = value; NotifyOfPropertyChange(() => Range);
            }
        }

        public List<String> SamplingCaps { get { return _samplingCaps; } set { _samplingCaps = value; NotifyOfPropertyChange(() => SamplingCaps); } }

        public int SelectedSamplingCapIndex
        {
            get { return _samplingCapIndex; }
            set
            {
                _samplingCapIndex = value;
                NotifyOfPropertyChange(() => SelectedSamplingCapIndex);
            }
        }

        #endregion

        #region YAxisControls

        /// <summary>
        /// The value of the lower Y Axis range
        /// </summary>
        public double Minimum { get { return _minimum; } set { _minimum = value; NotifyOfPropertyChange(() => Minimum); NotifyOfPropertyChange(() => MinimumValue); MinimumMaximum = Minimum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1; } }

        /// <summary>
        /// The minimum value as a readable string
        /// </summary>
        public string MinimumValue
        {
            get { return string.Format("{0:N2}", Minimum); }
            set
            {
                var old = Minimum;
                try
                {
                    Minimum = double.Parse(value);
                }
                catch (Exception)
                {
                    Minimum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the bottom range can reach
        /// </summary>
        public double MaximumMinimum { get { return _maximumMinimum; } set { _maximumMinimum = value; NotifyOfPropertyChange(() => MaximumMinimum); } }

        /// <summary>
        /// The lowest value the bottom range can reach
        /// </summary>
        public double MinimumMinimum { get { return _minimumMinimum; } set { _minimumMinimum = value; NotifyOfPropertyChange(() => MinimumMinimum); } }

        /// <summary>
        /// The value of the high Y Axis range
        /// </summary>
        public double Maximum { get { return _maximum; } set { _maximum = value; NotifyOfPropertyChange(() => Maximum); NotifyOfPropertyChange(() => MaximumValue); MaximumMinimum = Maximum; Range = new DoubleRange(Minimum, Maximum); if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1; } }

        /// <summary>
        /// The maximum value as a readable string
        /// </summary>
        public string MaximumValue
        {
            get { return string.Format("{0:N2}", Maximum); }
            set
            {
                var old = Maximum;
                try
                {
                    Maximum = double.Parse(value);
                }
                catch (Exception)
                {
                    Maximum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the top range can reach
        /// </summary>
        public double MaximumMaximum { get { return _maximumMaximum; } set { _maximumMaximum = value; NotifyOfPropertyChange(() => MaximumMaximum); } }

        /// <summary>
        /// The lowest value the top range can reach
        /// </summary>
        public double MinimumMaximum { get { return _minimumMaximum; } set { _minimumMaximum = value; NotifyOfPropertyChange(() => MinimumMaximum); } }

        #endregion

        #region Event Handlers

        public void SelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (string item in e.RemovedItems)
            {
                SelectedValues.Remove(DateTime.Parse(item));
            }

            foreach (string item in e.AddedItems)
            {
                SelectedValues.Add(DateTime.Parse(item));
            }
            NotifyOfPropertyChange(() => UndoButtonEnabled);

        }

        public void btnUndo()
        {
            if (_sensor != null)
            {
                _sensor.Undo();

                UpdateUndoRedo();
            }
        }

        public void btnRedo()
        {
            if (_sensor != null)
            {
                _sensor.Redo();

                UpdateUndoRedo();
            }
        }

        public void btnDone()
        {
            this.TryClose();
        }

        public void btnMakeZero()
        {
            EventLogger.LogInfo(_ds, GetType().ToString(), "Value updation started.");

            if (_selectedValues.Count == 0)
                return;

            _sensor.AddState(_sensor.CurrentState.MakeZero(SelectedValues));

            Finalise("Selected values set to 0.");

            Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);

            UpdateUndoRedo();
        }



        public void btnSpecify()
        {
            //TODO refactor
            EventLogger.LogInfo(_ds, GetType().ToString(), "Value updation started.");

            if (_selectedValues.Count == 0)
                return;

            var value = float.MinValue;

            while (value.Equals(float.MinValue))
            {
                try
                {
                    var specifyVal = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
                    _windowManager.ShowDialog(specifyVal);
                    //cancel
                    if (specifyVal.Text == null)
                        return;
                    value = float.Parse(specifyVal.Text);
                }
                catch (FormatException)
                {
                    var exit = Common.ShowMessageBox("An Error Occured", "Please enter a valid number.", true, true);
                    if (exit) return;
                }
            }

            ViewCursor = Cursors.Wait;
            _sensor.AddState(_sensor.CurrentState.MakeValue(SelectedValues, value));

            Finalise("Selected values has been set to " + value + ".");

            Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);

            UpdateUndoRedo();

            ViewCursor = Cursors.Arrow;
        }

        private void Finalise(string taskPerformed)
        {
            _missingValues = _sensor.CurrentState.GetMissingTimes(_ds.DataInterval, _ds.StartTimeStamp, _ds.EndTimeStamp);
            NotifyOfPropertyChange(() => MissingValues);
            NotifyOfPropertyChange(() => MissingCount);

            Common.RequestReason(_sensor, _container, _windowManager, taskPerformed);
        }

        public void btnInterpolate()
        {
            EventLogger.LogInfo(_ds, GetType().ToString(), "Value extrapolation invoked.");

            if (SelectedSensor == null)
            {
                Common.ShowMessageBox("No sensor selected", "Please choose a sensor before performing extrapolation.",
                                      false, true);
                return;
            }

            if (SelectedValues.Count == 0)
            {
                Common.ShowMessageBox("No values selected",
                                      "Please select one or more data points before performing extrapolation.",
                                      false, true);
                return;
            }

            if (
                !Common.ShowMessageBox("Interpolate",
                                       "This will find the first and last value in the current range and interpolate between them.\r\n\r\nAre you sure you want to do this?",
                                       true, false))
                return;

            ViewCursor = Cursors.Wait;

            try
            {
                var newState = SelectedSensor.CurrentState.Interpolate(SelectedValues, Dataset);
                SelectedSensor.AddState(newState);

                Finalise("Value extrapolation performed.");

                Common.ShowMessageBox("Values updated", "The values have been interpolated successfully.", false, false);
            }
            catch (DataException de)
            {
                if (de.Message == "No end value")
                {
                    Common.ShowMessageBox("Extrapolation", "There was no end value found, please specify a value", false,
                                          true);
                    _selectedValues.Clear();
                    _selectedValues.Add(_ds.EndTimeStamp);
                    SpecifyValueForExtrapolation();
                    _selectedValues.Clear();
                    _selectedValues.Add(_ds.EndTimeStamp.AddMinutes(-(_ds.DataInterval)));
                    btnInterpolate();
                }
                else if (de.Message == "No start value")
                {
                    Common.ShowMessageBox("Extrapolation", "There was no start value found, please specify a value",
                                          false, true);
                    _selectedValues.Clear();
                    _selectedValues.Add(_ds.StartTimeStamp);
                    SpecifyValueForExtrapolation();
                    _selectedValues.Clear();
                    _selectedValues.Add(_ds.StartTimeStamp.AddMinutes(_ds.DataInterval));
                    btnInterpolate();
                }

            }
            catch (Exception e)
            {
                Common.ShowMessageBoxWithException("Error",
                                                   "An error occured during extrapolation. Ensure you have selected a sensor and one or more data points, and try again.",
                                                   false, true, e);
                ViewCursor = Cursors.Arrow;
            }

            UpdateUndoRedo();
            ViewCursor = Cursors.Arrow;
        }

        private void SpecifyValueForExtrapolation()
        {
            var value = float.MinValue;

            while (value.Equals(float.MinValue))
            {
                try
                {
                    var specifyVal = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;
                    _windowManager.ShowDialog(specifyVal);
                    //cancel
                    if (specifyVal.Text == null)
                        return;
                    value = float.Parse(specifyVal.Text);
                }
                catch (FormatException)
                {
                    var exit = Common.ShowMessageBox("An Error Occured", "Please enter a valid number.", true, true);
                    if (exit) return;
                }
            }

            ViewCursor = Cursors.Wait;
            _sensor.AddState(_sensor.CurrentState.MakeValue(SelectedValues, value));

            UpdateUndoRedo();

            ViewCursor = Cursors.Arrow;
        }

        public void SamplingCapChanged(SelectionChangedEventArgs e)
        {
            try
            {
                Common.MaximumGraphablePoints = int.Parse((string)e.AddedItems[0]);
            }
            catch (Exception)
            {
                Common.MaximumGraphablePoints = int.MaxValue;
            }

            if (_graphableSensor != null)
                SampleValues(Common.MaximumGraphablePoints, new Collection<GraphableSensor> { _graphableSensor });
        }

        #endregion

        private void RefreshGraph()
        {
            if (_graphableSensor != null && _graphableSensor.DataPoints != null)
                _graphableSensor.RefreshDataPoints();
            UpdateGraph();
        }

        private void UpdateGraph()
        {
            if (SelectedSensor != null)
                SampleValues(Common.MaximumGraphablePoints, new Collection<GraphableSensor> { _graphableSensor });
            else
                ChartSeries = new List<LineSeries>();
            NotifyOfPropertyChange(() => ChartTitle);
            NotifyOfPropertyChange(() => YAxisTitle);
        }

        private void SampleValues(int numberOfPoints, ICollection<GraphableSensor> sensors)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            foreach (var sensor in sensors)
            {
                _sampleRate = sensor.DataPoints.Count() / (numberOfPoints / sensors.Count);
                Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate);

                var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
                generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
                if (_sampleRate > 1) ShowBackground();
            }

            ChartSeries = generatedSeries;

            MaximumMaximum = MaximumY().Y + 10;
            MinimumMinimum = MinimumY().Y - 10;

            Maximum = MaximumMaximum;
            Minimum = MinimumMinimum;
        }

        /// <summary>
        /// Calculates the maximum Y value in the graph
        /// </summary>
        /// <returns>The point containing the maximum Y value</returns>
        private DataPoint<DateTime, float> MaximumY()
        {
            DataPoint<DateTime, float> maxY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in (DataSeries<DateTime, float>)series.DataSeries)
                {
                    if (maxY == null)
                        maxY = value;
                    else if (value.Y > maxY.Y)
                        maxY = value;
                }
            }
            if (maxY == null)
                return new DataPoint<DateTime, float>(DateTime.Now, 10);
            return maxY;
        }

        /// <summary>
        /// Calculates the minimum Y value in the graph
        /// </summary>
        /// <returns>The point containing the minimum Y value</returns>
        private DataPoint<DateTime, float> MinimumY()
        {
            DataPoint<DateTime, float> minY = null;

            foreach (var series in ChartSeries)
            {
                foreach (var value in (DataSeries<DateTime, float>)series.DataSeries)
                {
                    if (minY == null)
                        minY = value;
                    else if (value.Y < minY.Y)
                        minY = value;
                }
            }
            if (minY == null)
                return new DataPoint<DateTime, float>(DateTime.Now, 0);
            return minY;
        }

        private void HideBackground()
        {
            _backgroundCanvas.Visibility = Visibility.Collapsed;
        }

        private void ShowBackground()
        {
            _backgroundCanvas.Visibility = Visibility.Visible;
        }

        #region Multi-level Undo/Redo Handling
        public void UndoPathSelected(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                var item = (SensorStateListObject)e.AddedItems[0];

                if (SelectedSensor != null && item != null)
                {
                    if (item.IsRaw)
                        SelectedSensor.RevertToRaw();
                    else
                        SelectedSensor.Undo(item.State.EditTimestamp);
                }

                ShowUndoStates = false;
                UpdateUndoRedo();
            }
        }

        public void RedoPathSelected(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                var item = (SensorStateListObject)e.AddedItems[0];

                if (SelectedSensor != null && item != null)
                    SelectedSensor.Redo(item.State.EditTimestamp);

                ShowRedoStates = false;
                UpdateUndoRedo();
            }
        }

        public void UpdateUndoRedo()
        {
            MissingValues = _sensor.CurrentState.GetMissingTimes(_ds.DataInterval, _ds.StartTimeStamp,
                                                     _ds.EndTimeStamp);
            RefreshGraph();

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
            NotifyOfPropertyChange(() => UndoStates);
            NotifyOfPropertyChange(() => RedoStates);
            NotifyOfPropertyChange(() => ShowUndoStates);
            NotifyOfPropertyChange(() => ShowRedoStates);
        }

        public bool ShowUndoStates { get; set; }

        public bool ShowRedoStates { get; set; }

        public ReadOnlyCollection<SensorStateListObject> UndoStates
        {
            get
            {
                var ss = new List<SensorStateListObject>();

                var atStart = true;

                foreach (var obj in _sensor.UndoStates)
                {
                    if (atStart)
                    {
                        atStart = false;
                        continue;
                    }

                    ss.Add(new SensorStateListObject(obj, false));
                }

                ss.Add(new SensorStateListObject(_sensor.RawData, true));

                return new ReadOnlyCollection<SensorStateListObject>(ss);
            }
        }

        public ReadOnlyCollection<SensorStateListObject> RedoStates
        {
            get
            {
                var ss = new List<SensorStateListObject>();

                foreach (var obj in _sensor.RedoStates)
                    ss.Add(new SensorStateListObject(obj, false));

                return new ReadOnlyCollection<SensorStateListObject>(ss);
            }
        }
        #endregion
    }
}
