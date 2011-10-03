using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    internal class CalibrateSensorsViewModel : BaseViewModel
    {
        #region Private Members

        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;

        private Sensor _sensor;
        private Dataset _ds;

        private string _formulaText = "";
        private bool _validFormula = true;

        private FormulaEvaluator _eval;
        private CompilerResults _results;

        private int _sampleRate;
        private int _zoomLevel = 100;
        private DateTime _startDateTime, _endDateTime;
        private Cursor _viewCursor = Cursors.Arrow;
        private List<LineSeries> _lineSeries;
        private DataSeries<DateTime, float> _dataSeries;
        private GraphableSensor _sensorToGraph;

        private Canvas _backgroundCanvas;
        private BehaviourManager _behaviour;
        private CustomZoomBehaviour _zoomBehav;

        private DoubleRange _range;
        private double _minimum;
        private double _minimumMinimum;
        private double _maximumMinimum;
        private double _maximum;
        private double _minimumMaximum;
        private double _maximumMaximum;
        private List<String> _samplingCaps = new List<string>();
        private int _samplingCapIndex;

        #endregion

        public CalibrateSensorsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;
            _eval = new FormulaEvaluator();

            _backgroundCanvas = new Canvas() {Visibility = Visibility.Collapsed};

            _behaviour = new BehaviourManager() {AllowMultipleEnabled = true};

            _behaviour.Behaviours.Add(new GraphBackgroundBehaviour(_backgroundCanvas) {IsEnabled = true});

            _zoomBehav = new CustomZoomBehaviour() {IsEnabled = true};
            _zoomBehav.ZoomRequested += (o, e) =>
                                            {
                                                // Set the DateTime ranges for calibration via visual selection
                                                UpdateGraphToShowRange((DateTime) e.FirstPoint.X,
                                                                       (DateTime) e.SecondPoint.X);
                                            };
            _zoomBehav.ZoomResetRequested += (o) =>
                                                 {
                                                     StartTime = _ds.StartTimeStamp;
                                                     EndTime = _ds.EndTimeStamp;

                                                     SensorToGraph.RemoveBounds();
                                                 };

            _behaviour.Behaviours.Add(_zoomBehav);

            Behaviour = _behaviour;

            SamplingCaps = new List<string>(Common.GenerateSamplingCaps());
            SelectedSamplingCapIndex = 3;
        }

        #region View Properties

        public BehaviourManager Behaviour
        {
            get { return _behaviour; }
            set
            {
                _behaviour = value;
                NotifyOfPropertyChange(() => Behaviour);
            }
        }

        public DataSeries<DateTime, float> DataSeries
        {
            get { return _dataSeries; }
            set
            {
                _dataSeries = value;
                NotifyOfPropertyChange(() => DataSeries);
            }
        }

        public List<LineSeries> ChartSeries
        {
            get { return _lineSeries; }
            set
            {
                _lineSeries = value;
                NotifyOfPropertyChange(() => ChartSeries);
            }
        }

        public GraphableSensor SensorToGraph
        {
            get { return _sensorToGraph; }
            set { _sensorToGraph = value; }
        }

        public string ChartTitle { get; set; }
        public string YAxisTitle { get; set; }

        public Brush FormulaBoxBackground
        {
            get
            {
                return ValidFormula
                           ? new SolidColorBrush(Colors.White)
                           : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
            }
        }

        public string FormulaText
        {
            get { return _formulaText; }
            set
            {
                _formulaText = value;
                NotifyOfPropertyChange(() => FormulaText);

                if (!IndiaTango.Properties.Settings.Default.EvaluateFormulaOnKeyUp)
                    return;

                //Uncomment for per character validity checking
                //ValidFormula = _eval.ParseFormula(value);
                //Console.WriteLine("Formual Validity: " + _validFormula);

                //Uncoment for per character compile checking
                _results = _eval.CompileFormula(FormulaText);
                ValidFormula = _eval.CheckCompileResults(_results);
            }
        }

        public bool ValidFormula
        {
            get { return _validFormula; }
            set
            {
                _validFormula = value;

                NotifyOfPropertyChange(() => ValidFormula);
                NotifyOfPropertyChange(() => FormulaBoxBackground);
                NotifyOfPropertyChange(() => ApplyButtonEnabled);
            }
        }

        public bool RedoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.RedoStates.Count > 0; }
        }

        public bool UndoButtonEnabled
        {
            get { return SelectedSensor != null && !SelectedSensor.CurrentState.IsRaw; }
        }

        public bool ApplyButtonEnabled
        {
            get
            {
                return SelectedSensor != null &&
                       (ValidFormula || !IndiaTango.Properties.Settings.Default.EvaluateFormulaOnKeyUp);
            }
        }

        public int ZoomLevel
        {
            get { return _zoomLevel; }
            set
            {
                _zoomLevel = Math.Max(100, value);
                _zoomLevel = Math.Min(1000, _zoomLevel);

                NotifyOfPropertyChange(() => ZoomLevel);
                NotifyOfPropertyChange(() => ZoomText);

                //TODO: Actually zoom
            }
        }

        public Dataset Dataset
        {
            get { return _ds; }
            set
            {
                _ds = value;
                StartTime = _ds.StartTimeStamp;
                EndTime = _ds.EndTimeStamp;
            }
        }

        public string ZoomText
        {
            get { return ZoomLevel + "%"; }
        }

        public string Title
        {
            get { return "Calibrate Sensors" + (SelectedSensor != null ? " - " + SelectedSensor.Name : ""); }
        }

        public String SensorName
        {
            get { return SelectedSensor == null ? "" : SelectedSensor.Name; }
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

                YAxisTitle = SelectedSensor.Unit;
                ChartTitle = SelectedSensor.Name;

                SensorToGraph = new GraphableSensor(SelectedSensor);

                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => ChartTitle);
                NotifyOfPropertyChange(() => YAxisTitle);
                NotifyOfPropertyChange(() => SensorToGraph);
                NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => UndoButtonEnabled);
                NotifyOfPropertyChange(() => RedoButtonEnabled);
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => CanEditDates);
                NotifyOfPropertyChange(() => ApplyButtonEnabled);

                RefreshGraph();
            }
        }

        public DateTime StartTime
        {
            get { return _startDateTime; }
            set
            {
                _startDateTime = value;

                //if (_ds != null && value >= _ds.StartTimeStamp && value <= EndTime)
                //    _startDateTime = value;
                //else if(_ds != null)
                //    Common.ShowMessageBox("Incorrect Date",
                //                          "Start time must be after than or equal to the first date of the data set (" +
                //                          _ds.StartTimeStamp +
                //                          ") and before the end time (" + EndTime + ")", false, true);
                NotifyOfPropertyChange(() => StartTime);
            }
        }

        private void UpdateGraphToShowRange(DateTime start, DateTime end)
        {
            StartTime = start;
            EndTime = end;

            SensorToGraph.SetUpperAndLowerBounds(start, end);
            UpdateGraph();
        }

        public DateTime EndTime
        {
            get { return _endDateTime; }
            set
            {
                _endDateTime = value;


                //if (_ds != null && value <= _ds.EndTimeStamp && value >= StartTime)
                //    _endDateTime = value;
                //else if(_ds != null)
                //    Common.ShowMessageBox("Incorrect Date",
                //                          "End time must be before than or equal to the last date of the data set (" +
                //                          _ds.EndTimeStamp +
                //                          ") and after the start time (" + StartTime + ")", false, true);
                NotifyOfPropertyChange(() => EndTime);
            }
        }

        public bool CanEditDates
        {
            get { return (SelectedSensor != null); }
        }

        public Cursor ViewCursor
        {
            get { return _viewCursor; }
            set
            {
                _viewCursor = value;
                NotifyOfPropertyChange(() => ViewCursor);
            }
        }

        public DoubleRange Range
        {
            get { return _range; }
            set
            {
                _range = value;
                NotifyOfPropertyChange(() => Range);
            }
        }

        public List<String> SamplingCaps
        {
            get { return _samplingCaps; }
            set
            {
                _samplingCaps = value;
                NotifyOfPropertyChange(() => SamplingCaps);
            }
        }

        public int SelectedSamplingCapIndex
        {
            get { return _samplingCapIndex; }
            set
            {
                _samplingCapIndex = value;
                NotifyOfPropertyChange(() => SelectedSamplingCapIndex);
            }
        }

        #region YAxisControls

        /// <summary>
        /// The value of the lower Y Axis range
        /// </summary>
        public double Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                NotifyOfPropertyChange(() => Minimum);
                NotifyOfPropertyChange(() => MinimumValue);
                MinimumMaximum = Minimum;
                Range = new DoubleRange(Minimum, Maximum);
                if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1;
            }
        }

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
                catch (Exception e)
                {
                    Minimum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the bottom range can reach
        /// </summary>
        public double MaximumMinimum
        {
            get { return _maximumMinimum; }
            set
            {
                _maximumMinimum = value;
                NotifyOfPropertyChange(() => MaximumMinimum);
            }
        }

        /// <summary>
        /// The lowest value the bottom range can reach
        /// </summary>
        public double MinimumMinimum
        {
            get { return _minimumMinimum; }
            set
            {
                _minimumMinimum = value;
                NotifyOfPropertyChange(() => MinimumMinimum);
            }
        }

        /// <summary>
        /// The value of the high Y Axis range
        /// </summary>
        public double Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                NotifyOfPropertyChange(() => Maximum);
                NotifyOfPropertyChange(() => MaximumValue);
                MaximumMinimum = Maximum;
                Range = new DoubleRange(Minimum, Maximum);
                if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1;
            }
        }

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
                catch (Exception e)
                {
                    Maximum = old;
                }
            }
        }

        /// <summary>
        /// The highest value the top range can reach
        /// </summary>
        public double MaximumMaximum
        {
            get { return _maximumMaximum; }
            set
            {
                _maximumMaximum = value;
                NotifyOfPropertyChange(() => MaximumMaximum);
            }
        }

        /// <summary>
        /// The lowest value the top range can reach
        /// </summary>
        public double MinimumMaximum
        {
            get { return _minimumMaximum; }
            set
            {
                _minimumMaximum = value;
                NotifyOfPropertyChange(() => MinimumMaximum);
            }
        }

        #endregion

        #endregion

        #region Event Handlers

        public void StartTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            // Is start within bounds?
            var newValue = (DateTime) e.NewValue;

            if (newValue.CompareTo(EndTime) >= 0 || !inDatasetRange(newValue))
            {
                // Revert
                StartTime = (DateTime) e.OldValue;
                return;
            }

            UpdateGraphToShowRange(StartTime, EndTime);
        }

        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            // Is end within bounds?
            var newValue = (DateTime) e.NewValue;

            if (newValue.CompareTo(StartTime) <= 0 || !inDatasetRange(newValue))
            {
                // Revert
                EndTime = (DateTime) e.OldValue;
                return;
            }

            UpdateGraphToShowRange(StartTime, EndTime);
        }

        private bool inDatasetRange(DateTime val)
        {
            return (val.CompareTo(_ds.StartTimeStamp) >= 0 && val.CompareTo(_ds.EndTimeStamp) <= 0);
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

        public void btnHelp()
        {
            string message = "";

            message =
                "The program applies the formula entered across all sensors data points within the specified range.\n" +
                "The following gives an indication of the operations and syntax.\n\n" +
                "Mathematical operations\t [ -, +, *, ^, % ]\n" +
                "Mathematical functions\t [ Sin(y), Cos(y), Tan(y), Pi ]\n\n" +
                "To set a data points value for a particular sensor, use that sensors variable followed an equals sign, then by the value.\n" +
                "   eg: To set the values of the sensor Temperatue1 to 5 for all points, use 'a = 5' \n\n" +
                "To use a sensors values in a calculation, use that sesnors variable.\n" +
                "   eg: To make all the values of the sensor Temperature1 equal to Temperature2, use a = b\n\n" +
                "To use the data points time stamp in calculations use 't.' followed by the time part desired.\n" +
                "   eg: t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second\n\n" +
                "Examples:\n" +
                "'x = x + 1'\n" +
                "'x = t.Date'\n" +
                "'x = x * Cos(x + 1) + 2'";
            Common.ShowMessageBox("Formula Help", message, false, false);
        }

        public void btnApply()
        {
            _results = _eval.CompileFormula(FormulaText);
            ValidFormula = _eval.CheckCompileResults(_results);
            DateTime t = DateTime.Now;

            if (ValidFormula)
            {
                ViewCursor = Cursors.Wait;
                SensorState newState = _eval.EvaluateFormula(_results, SelectedSensor.CurrentState.Clone(),
                                                             _ds.StartTimeStamp,
                                                             _ds.EndTimeStamp);

                SelectedSensor.AddState(newState);

                ViewCursor = Cursors.Arrow;

                Common.RequestReason(SelectedSensor, _container, _windowManager, SelectedSensor.CurrentState, "Formula '" + FormulaText + "' successfully applied to the sensor.");

                Common.ShowMessageBox("Formula applied", "The formula was successfully applied to the selected sensor.",
                                      false, false);
            }
            else
            {
                string errorString = "";

                if (_results.Errors.Count > 0)
                    foreach (CompilerError error in _results.Errors)
                        errorString += error.ErrorText + "\n";

                Common.ShowMessageBoxWithExpansion("Unable to Apply Formula",
                                                   "An error was encounted when trying to apply the formula.\nPlease check the formula syntax.",
                                                   false, true, errorString);
            }

            UpdateUndoRedo();
        }

        public void btnClear()
        {
            FormulaText = "";
        }

        public void SamplingCapChanged(SelectionChangedEventArgs e)
        {
            try
            {
                Common.MaximumGraphablePoints = int.Parse((string) e.AddedItems[0]);
            }
            catch (Exception)
            {
                Common.MaximumGraphablePoints = int.MaxValue;
            }

            if (_sensorToGraph != null)
                SampleValues(Common.MaximumGraphablePoints, _sensorToGraph);
        }

        #endregion

        #region Graph stuffs

        private void RefreshGraph()
        {
            if (SensorToGraph != null && SensorToGraph.DataPoints != null)
                SensorToGraph.RefreshDataPoints();

            UpdateGraph();
        }

        private void UpdateGraph()
        {
            if (SensorToGraph != null)
                SampleValues(Common.MaximumGraphablePoints, SensorToGraph);
            else
                ChartSeries = new List<LineSeries>();

            NotifyOfPropertyChange(() => ChartTitle);
            NotifyOfPropertyChange(() => YAxisTitle);
        }

        // Only need one graph at a time here
        private void SampleValues(int numberOfPoints, GraphableSensor sensor)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            _sampleRate = sensor.DataPoints.Count()/(numberOfPoints);
            Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(),
                        numberOfPoints, _sampleRate);

            var series = (_sampleRate > 1)
                             ? new DataSeries<DateTime, float>(sensor.Sensor.Name,
                                                               sensor.DataPoints.Where(
                                                                   (x, index) => index%_sampleRate == 0))
                             : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
            generatedSeries.Add(new LineSeries {DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour)});

            if (_sampleRate > 1) ShowBackground();

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
                foreach (var value in (DataSeries<DateTime, float>) series.DataSeries)
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
                foreach (var value in (DataSeries<DateTime, float>) series.DataSeries)
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

        #endregion

        #region Multi-level Undo/Redo handling
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
                        continue; // Initial state should NOT be listed - it is the current state
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

        private void UpdateUndoRedo()
        {
            RefreshGraph();

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
            NotifyOfPropertyChange(() => UndoStates);
            NotifyOfPropertyChange(() => RedoStates);
            NotifyOfPropertyChange(() => ShowUndoStates);
            NotifyOfPropertyChange(() => ShowRedoStates);
        }
        #endregion
    }
}
