using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
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

        private SensorVariable _sensor;
        private Dataset _ds;

        private string _formulaText = "";
        private bool _validFormula = false;

        private FormulaEvaluator _eval;
        private Formula _formula;
        private List<SensorVariable> _sensorVariables;

        private int _sampleRate;
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

        private bool _useManualCalibration = true;
        private string _calAText = "";
        private string _calBText = "";
        private string _curAText = "";
        private string _curBText = "";
        private double _calAValue = 0;
        private double _calBValue = 0;
        private double _curAValue = 0;
        private double _curBValue = 0;
        private bool _calAValid;
        private bool _calBValid;
        private bool _curAValid;
        private bool _curBValid;


        #endregion

        public CalibrateSensorsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;


            _backgroundCanvas = new Canvas() { Visibility = Visibility.Collapsed };

            _behaviour = new BehaviourManager() { AllowMultipleEnabled = true };

            _behaviour.Behaviours.Add(new GraphBackgroundBehaviour(_backgroundCanvas) { IsEnabled = true });

            _zoomBehav = new CustomZoomBehaviour() { IsEnabled = true };
            _zoomBehav.ZoomRequested += (o, e) =>
                                            {
                                                // Set the DateTime ranges for calibration via visual selection
                                                UpdateGraphToShowRange((DateTime)e.FirstPoint.X,
                                                                       (DateTime)e.SecondPoint.X);
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

                if (Properties.Settings.Default.EvaluateFormulaOnKeyUp)
                    return;

                //Uncomment for per character validity checking
                //ValidFormula = _eval.ParseFormula(value);
                //Console.WriteLine("Formual Validity: " + _validFormula);

                //Uncoment for per character compile checking
                if (string.IsNullOrWhiteSpace(_formulaText))
                {
                    ValidFormula = false;
                    return;
                }

                _formula = _eval.CompileFormula(FormulaText);
                ValidFormula = _formula.IsValid;
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

        public bool AutoCalibrationEnabled
        {
            get { return SelectedSensor != null && _calAValid && _calBValid && _curAValid && _curBValid; }
        }

        public bool UseManualCalibration
        {
            get { return _useManualCalibration; }
            set
            {
                _useManualCalibration = value;
                NotifyOfPropertyChange(() => UseManualCalibration);
                NotifyOfPropertyChange(() => SelectedTabIndex);
            }
        }

        public int SelectedTabIndex
        {
            get { return UseManualCalibration ? 0 : 1; }
            set { UseManualCalibration = value == 0; }
        }

        public string CalAText
        {
            get { return _calAText; }
            set
            {
                _calAText = value;
                _calAValid = double.TryParse(CalAText, out _calAValue);
                NotifyOfPropertyChange(() => CalAText);
                NotifyOfPropertyChange(() => CalABackground);
                NotifyOfPropertyChange(() => AutoCalibrationEnabled);

                UpdateGraph();
            }
        }

        public Brush CalABackground
        {
            get
            {
                return _calAValid
                         ? new SolidColorBrush(Colors.White)
                         : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
            }
        }

        public string CalBText
        {
            get { return _calBText; }
            set
            {
                _calBText = value;
                _calBValid = double.TryParse(CalBText, out _calBValue);
                NotifyOfPropertyChange(() => CalBText);
                NotifyOfPropertyChange(() => CalBBackground);
                NotifyOfPropertyChange(() => AutoCalibrationEnabled);

                UpdateGraph();
            }
        }

        public Brush CalBBackground
        {
            get
            {
                return _calBValid
                         ? new SolidColorBrush(Colors.White)
                         : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
            }
        }

        public string CurAText
        {
            get { return _curAText; }
            set
            {
                _curAText = value;
                _curAValid = double.TryParse(CurAText, out _curAValue);
                NotifyOfPropertyChange(() => CurAText);
                NotifyOfPropertyChange(() => CurABackground);
                NotifyOfPropertyChange(() => AutoCalibrationEnabled);

                UpdateGraph();
            }
        }

        public Brush CurABackground
        {
            get
            {
                return _curAValid
                         ? new SolidColorBrush(Colors.White)
                         : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
            }
        }

        public string CurBText
        {
            get { return _curBText; }
            set
            {
                _curBText = value;
                _curBValid = double.TryParse(CurBText, out _curBValue);
                NotifyOfPropertyChange(() => CurBText);
                NotifyOfPropertyChange(() => CurBBackground);
                NotifyOfPropertyChange(() => AutoCalibrationEnabled);

                UpdateGraph();
            }
        }

        public Brush CurBBackground
        {
            get
            {
                return _curBValid
                         ? new SolidColorBrush(Colors.White)
                         : new SolidColorBrush(Color.FromArgb(126, 255, 69, 0));
            }
        }


        public bool RedoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.Sensor.RedoStates.Count > 0; }
        }

        public bool UndoButtonEnabled
        {
            get { return SelectedSensor != null && !SelectedSensor.Sensor.CurrentState.IsRaw; }
        }

        public bool ApplyButtonEnabled
        {
            get
            {
                return ValidFormula || Properties.Settings.Default.EvaluateFormulaOnKeyUp;
            }
        }

        public Dataset Dataset
        {
            get { return _ds; }
            set
            {
                _ds = value;
                _eval = new FormulaEvaluator(_ds.Sensors, _ds.DataInterval);
                StartTime = _ds.StartTimeStamp;
                EndTime = _ds.EndTimeStamp;
                _sensorVariables = SensorVariable.CreateSensorVariablesFromSensors(_ds.Sensors);
            }
        }

        public string Title
        {
            get { return string.Format("[{1}] Calibrate Sensors{0}", (SelectedSensor != null ? " - " + SelectedSensor.Sensor.Name : ""), (Dataset != null ? Dataset.IdentifiableName : Common.UnknownSite)); }
        }

        public String SensorName
        {
            get { return SelectedSensor == null ? "" : SelectedSensor.Sensor.Name; }
        }

        public List<SensorVariable> SensorList
        {
            get { return _sensorVariables; }
            set
            {
                _sensorVariables = value;
                NotifyOfPropertyChange(() => SensorList);
            }
        }

        public SensorVariable SelectedSensor
        {
            get { return _sensor; }
            set
            {
                _sensor = value;

                YAxisTitle = SelectedSensor.Sensor.Unit;
                ChartTitle = SelectedSensor.Sensor.Name;

                SensorToGraph = new GraphableSensor(SelectedSensor.Sensor);

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
            get { return (true); }
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
                if (Math.Abs(Maximum - Minimum) < 0.001) Minimum -= 1;
                if (Minimum < Maximum)
                    Range = new DoubleRange(Minimum, Maximum);
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
                catch (Exception)
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
                if (Math.Abs(Maximum - Minimum) < 0.001) Maximum += 1;
                if (Minimum < Maximum)
                    Range = new DoubleRange(Minimum, Maximum);
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
                catch (Exception)
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
            var newValue = (DateTime)e.NewValue;

            if (newValue.CompareTo(EndTime) >= 0 || !inDatasetRange(newValue))
            {
                // Revert
                StartTime = (DateTime)e.OldValue;
                return;
            }

            UpdateGraphToShowRange(StartTime, EndTime);
        }

        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            // Is end within bounds?
            var newValue = (DateTime)e.NewValue;

            if (newValue.CompareTo(StartTime) <= 0 || !inDatasetRange(newValue))
            {
                // Revert
                EndTime = (DateTime)e.OldValue;
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
                _sensor.Sensor.Undo();
                UpdateUndoRedo();
            }
        }

        public void btnRedo()
        {
            if (_sensor != null)
            {
                _sensor.Sensor.Redo();
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
                "To set a data points value for a particular sensor, use that sensors variable followed by a space and an equals sign, then by the value.\n" +
                "   eg: To set the values of the sensor " + _sensorVariables[0].Sensor.Name + " to 5 for all points, use '" + _sensorVariables[0].VariableName + " = 5' \n\n" +
                "To use a sensors values in a calculation, use that sesnors variable.\n" +
                "   eg: To make all the values of the sensor " + _sensorVariables[0].Sensor.Name + " equal to " + _sensorVariables[1].Sensor.Name +
                    ", use " + _sensorVariables[0].VariableName + " = " + _sensorVariables[1].VariableName + "\n\n" +
                "To use the data points time stamp in calculations use 'time.' followed by the time part desired.\n" +
                "   eg: time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second\n\n" +
                "Examples:\n" +
                "'x = x + 1'\n" +
                "'x = time.Date'\n" +
                "'x = x * Cos(x + 1) + 2'";
            Common.ShowMessageBox("Formula Help", message, false, false);
        }

        public void btnApplyFormula()
        {
            if (string.IsNullOrWhiteSpace(_formulaText))
            {
                ValidFormula = false;
                return;
            }

            _formula = _eval.CompileFormula(FormulaText);
            ValidFormula = _formula.IsValid;
            DateTime t = DateTime.Now;

            if (ValidFormula)
            {
                bool skipMissingValues = false;
                string missingSensors = "";
                MissingValuesDetector detector = new MissingValuesDetector();

                //Detect if missing values
                foreach (var sensorVariable in _formula.SensorsUsed)
                {
                    if (detector.GetDetectedValues(sensorVariable.Sensor).Count > 0)
                        missingSensors += "\t" + sensorVariable.Sensor.Name + " (" + sensorVariable.VariableName + ")\n";
                }

                if (missingSensors != "")
                {
                    string action = "";
                    var specify =
                        (SpecifyValueViewModel)_container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel");
                    specify.Title = "Missing Values Detected";
                    specify.Message =
                        "The following sensors you have used in the formula contain missing values:\n\n" + missingSensors + "\nPlease select an action to take.";
                    specify.ShowComboBox = true;
                    specify.ShowCancel = true;
                    specify.CanEditComboBox = false;
                    specify.ComboBoxItems =
                        new List<string>(new[] { "Treat all missing values as zero", "Skip over all missing values" });
                    specify.ComboBoxSelectedIndex = 0;
                    specify.Deactivated += (o, e) =>
                                            {
                                                action = specify.Text;
                                            };

                    _windowManager.ShowDialog(specify);

                    if (specify.WasCanceled) return;
                    skipMissingValues = specify.ComboBoxSelectedIndex == 1;
                }

                ViewCursor = Cursors.Wait;
                _eval.EvaluateFormula(_formula, StartTime, EndTime, skipMissingValues);

                ViewCursor = Cursors.Arrow;

                Common.RequestReason(SensorVariable.CreateSensorsFromSensorVariables(_formula.SensorsUsed), _container, _windowManager, "Formula '" + FormulaText + "' successfully applied to the sensor.");

                Common.ShowMessageBox("Formula applied", "The formula was successfully applied to the selected sensor.",
                                      false, false);

                UpdateUndoRedo();
                UpdateGraph();
            }
            //Not Valid
            else
            {
                string errorString = "";

                if (_formula.CompilerResults.Errors.Count > 0)
                    foreach (CompilerError error in _formula.CompilerResults.Errors)
                        errorString += error.ErrorText + "\n";

                Common.ShowMessageBoxWithExpansion("Unable to Apply Formula",
                                                   "An error was encounted when trying to apply the formula.\nPlease check the formula syntax.",
                                                   false, true, errorString);
            }
        }

        public void btnApplyAuto()
        {
            if (SelectedSensor != null)
            {
                try
                {
                    SelectedSensor.Sensor.AddState(SelectedSensor.Sensor.CurrentState.Calibrate(StartTime, EndTime,
                                                                                                _calAValue, _calBValue,
                                                                                                _curAValue, _curBValue));

                    Common.RequestReason(SelectedSensor.Sensor, _container, _windowManager, "Calibration CalA='" + _calBValue + "', CalB='" + _calBValue + "', CurA='" + _curAValue + "', CurB='" + _curBValue + "' successfully applied to the sensor.");

                }
                catch (Exception ex)
                {
                    Common.ShowMessageBox("An Error Occured", ex.Message, false, true);
                }
            }

            UpdateUndoRedo();
            UpdateGraph();
        }

        public void btnClearFormula()
        {
            FormulaText = "";
        }

        public void btnClearAuto()
        {
            CalAText = "";
            CalBText = "";
            CurAText = "";
            CurBText = "";
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

            _sampleRate = sensor.DataPoints.Count() / (numberOfPoints);
            Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(),
                        numberOfPoints, _sampleRate);

            var series = (_sampleRate > 1)
                             ? new DataSeries<DateTime, float>(sensor.Sensor.Name,
                                                               sensor.DataPoints.Where(
                                                                   (x, index) => index % _sampleRate == 0))
                             : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
            generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });

            if(SelectedSensor != null && _calAValid && _curAValid)generatedSeries.Add(new LineSeries
            {
                DataSeries =
                    new DataSeries<DateTime, float>("A Calibration Line",
                                                    new List<DataPoint<DateTime, float>>()
                                                        {
                                                            new DataPoint<DateTime, float>(
                                                                StartTime, (float) _calAValue),
                                                            new DataPoint<DateTime, float>(
                                                                EndTime, (float) _curAValue)
                                                        }),
                LineStroke = new SolidColorBrush(Colors.OrangeRed)
            });

            if(SelectedSensor != null && _calBValid && _curBValid)generatedSeries.Add(new LineSeries
            {
                DataSeries =
                    new DataSeries<DateTime, float>("B Calibration Line",
                                                    new List<DataPoint<DateTime, float>>()
                                                                                {
                                                                                    new DataPoint<DateTime, float>(
                                                                                        StartTime, (float) _calBValue),
                                                                                    new DataPoint<DateTime, float>(
                                                                                        EndTime, (float) _curBValue)
                                                                                }),
                LineStroke = new SolidColorBrush(Colors.OrangeRed)
            });

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
                        SelectedSensor.Sensor.RevertToRaw();
                    else
                        SelectedSensor.Sensor.Undo(item.State.EditTimestamp);
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
                    SelectedSensor.Sensor.Redo(item.State.EditTimestamp);

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

                foreach (var obj in _sensor.Sensor.UndoStates)
                {
                    if (atStart)
                    {
                        atStart = false;
                        continue; // Initial state should NOT be listed - it is the current state
                    }

                    ss.Add(new SensorStateListObject(obj, false));
                }

                ss.Add(new SensorStateListObject(_sensor.Sensor.RawData, true));

                return new ReadOnlyCollection<SensorStateListObject>(ss);
            }
        }

        public ReadOnlyCollection<SensorStateListObject> RedoStates
        {
            get
            {
                var ss = new List<SensorStateListObject>();

                foreach (var obj in _sensor.Sensor.RedoStates)
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
