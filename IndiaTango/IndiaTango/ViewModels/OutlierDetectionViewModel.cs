using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    internal class OutlierDetectionViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private List<DateTime> _selectedValues = new List<DateTime>();
        private List<DateTime> _outliers = new List<DateTime>();
        private Dataset _ds;
        private int _zoomLevel = 100;
        private Sensor _sensor;
        private bool _minMaxMode = true;
        private float _numStdDev = 1;
        private int _smoothingPeriod = 4;
		private Cursor _viewCursor = Cursors.Arrow;

        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private BehaviourManager _behaviour;
        private DoubleRange _range;
        private int _sampleRate;
        private GraphableSensor _graphableSensor;
        private Canvas _backgroundCanvas;

        private double _minimum;
        private double _minimumMinimum;
        private double _maximumMinimum;
        private double _maximum;
        private double _minimumMaximum;
        private double _maximumMaximum;
        private List<String> _samplingCaps = new List<string>();
        private int _samplingCapIndex;

        public OutlierDetectionViewModel(IWindowManager manager, SimpleContainer container)
        {
            _windowManager = manager;
            _container = container;

            _backgroundCanvas = new Canvas { Visibility = Visibility.Collapsed };

            _behaviour = new BehaviourManager { AllowMultipleEnabled = true };

            var backgroundBehaviour = new GraphBackgroundBehaviour(_backgroundCanvas) { IsEnabled = true };

            _behaviour.Behaviours.Add(backgroundBehaviour);

            var zoomBehaviour = new CustomZoomBehaviour { IsEnabled = true };
            zoomBehaviour.ZoomRequested += (o, e) =>
            {
                var startTime = (DateTime)e.FirstPoint.X;
                var endTime = (DateTime)e.SecondPoint.X;
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

		public Cursor ViewCursor
		{
			get { return _viewCursor; }
			set { _viewCursor = value; NotifyOfPropertyChange(() => ViewCursor); }
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
            set { _ds = value; }
        }

        public string ZoomText
        {
            get { return ZoomLevel + "%"; }
        }

        public String SensorName
        {
            get { return SelectedSensor == null ? "" : SelectedSensor.Name; }
        }

        public List<string> OutliersStrings
        {
            get
            {
                var list = new List<String>();
                foreach (var time in _outliers)
                {
                    list.Add(time.ToShortDateString().PadRight(12)+time.ToShortTimeString().PadRight(15) + _sensor.CurrentState.Values[time]);
                }
                return list;
            }
        }

        public List<DateTime> Outliers
        {
            get { return _outliers; }
            set
            {
                _outliers = value;
                NotifyOfPropertyChange(() => OutliersStrings);
            }
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
                Outliers = (_minMaxMode)
                               ? _sensor.CurrentState.GetOutliersFromMaxAndMin(_ds.DataInterval, _ds.StartTimeStamp,
                                                                               _ds.EndTimeStamp,
                                                                               _sensor.UpperLimit, _sensor.LowerLimit,
                                                                               _sensor.MaxRateOfChange)
                               : _sensor.CurrentState.GetOutliersFromStdDev(_ds.DataInterval, _ds.StartTimeStamp,
                                                                            _ds.EndTimeStamp, NumStdDev, _smoothingPeriod);
                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => OutliersStrings);
				NotifyOfPropertyChange(() => UndoButtonEnabled);
				NotifyOfPropertyChange(() => RedoButtonEnabled);
                _graphableSensor = _sensor != null ? new GraphableSensor(_sensor) : null;
                UpdateGraph();
            }
        }

        public List<DateTime> SelectedValues
        {
            get { return _selectedValues; }
            set
            {
                if(value!=null)
                    _selectedValues = value;
                else
                    _selectedValues = new List<DateTime>();
                NotifyOfPropertyChange(() => SelectedValues);
            }
        }

        public Boolean MinMaxMode
        {
            get { return _minMaxMode; }
            set 
            { 
                _minMaxMode = value;
                NotifyOfPropertyChange(()=>MinMaxMode);
                NotifyOfPropertyChange(() =>StdDevMode);
                NotifyOfPropertyChange(()=>OutliersStrings);
                NotifyOfPropertyChange(()=>SelectedSensor);
            }
        }

        public Boolean StdDevMode
        {
            get { return !_minMaxMode; }
            set
            {
                _minMaxMode = !value;
                NotifyOfPropertyChange(() => MinMaxMode);
                NotifyOfPropertyChange(() => StdDevMode);
                NotifyOfPropertyChange(() => OutliersStrings);
                NotifyOfPropertyChange(() => SelectedSensor);
            }
        }

        public float NumStdDev
        {
            get { return _numStdDev; }
            set
            {
                _numStdDev = value;
                NotifyOfPropertyChange(() => NumStdDev);
                NotifyOfPropertyChange(()=>SelectedSensor);
            }

        }

        public int MaxHours
        {
            get { return (int)Math.Floor(_ds.EndTimeStamp.Subtract(_ds.StartTimeStamp).TotalHours); }
        }

        public int SmoothingPeriod
        {
            get{return (int)Math.Ceiling(_smoothingPeriod / (60d / _ds.DataInterval));}
            set
            {

                _smoothingPeriod = value*(60/_ds.DataInterval);
                NotifyOfPropertyChange(() => SmoothingPeriod);
                NotifyOfPropertyChange(() => SelectedSensor);
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

        public List<LineSeries> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange(() => ChartSeries); } }

        public BehaviourManager Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        public string ChartTitle { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Name; } }

        public string YAxisTitle { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Unit; } }

        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }
        #endregion

        #region button event handlers

        public void SelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (string item in e.RemovedItems)
            {
                SelectedValues.Remove(DateTime.Parse(item.Substring(0, 27)));
            }

            foreach (string item in e.AddedItems)
            {
                SelectedValues.Add(DateTime.Parse(item.Substring(0, 27)));
            }

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        }


        public void btnRemove()
        {
            EventLogger.LogInfo(_ds, GetType().ToString(), "Value removal started.");
            if (_selectedValues.Count == 0)
                return;

        	ViewCursor = Cursors.Wait;
            _sensor.AddState(_sensor.CurrentState.RemoveValues(SelectedValues));

            Finalise("Removed selected values from dataset.");

            RefreshGraph();
            Common.ShowMessageBox("Values Updated", "The selected values have been removed from the data", false, false);

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        	ViewCursor = Cursors.Arrow;
        }

        public void btnMakeZero()
        {
            EventLogger.LogInfo(_ds, GetType().ToString(), "Value updation started.");

            if (_selectedValues.Count == 0)
                return;

			ViewCursor = Cursors.Wait;
            _sensor.AddState(_sensor.CurrentState.ChangeToZero(SelectedValues));

            Finalise("Set selected values to 0.");
            RefreshGraph();
            Common.ShowMessageBox("Values Updated", "The selected values have been set to 0.", false, false);

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
			ViewCursor = Cursors.Arrow;
        }

        private void Finalise(string taskPerformed)
        {
            UpdateUndoRedo();
            /*Outliers = (_minMaxMode)
                           ? _sensor.CurrentState.GetOutliersFromMaxAndMin(_ds.DataInterval, _ds.StartTimeStamp,
                                                                           _ds.EndTimeStamp,
                                                                           _sensor.UpperLimit, _sensor.LowerLimit,
                                                                           _sensor.MaxRateOfChange)
                           : _sensor.CurrentState.GetOutliersFromStdDev(_ds.DataInterval, _ds.StartTimeStamp,
                                                                        _ds.EndTimeStamp,NumStdDev,_smoothingPeriod);*/
            NotifyOfPropertyChange(() => Outliers);
            NotifyOfPropertyChange(() => OutliersStrings);

            Common.RequestReason(_sensor, _container, _windowManager, taskPerformed);
        }

        public void btnSpecify()
        {
            //TODO refactor
            EventLogger.LogInfo(_ds, GetType().ToString(), "Value updation started.");

            if (_selectedValues.Count == 0)
                return;

            var value = float.MinValue;

            while (value == float.MinValue)
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
            _sensor.AddState(_sensor.CurrentState.ChangeToValue(SelectedValues, value));

            Finalise("Specified values for selected data points as " + value + ".");

            RefreshGraph();
            Common.ShowMessageBox("Values Updated", "The selected values have been set to " + value + ".", false, false);

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
			ViewCursor = Cursors.Arrow;
        }

        public void btnUndo()
        {
            _sensor.Undo();
            UpdateUndoRedo();
        }

        public void btnRedo()
        {
            _sensor.Redo();
            UpdateUndoRedo();
        }

        public void btnDone()
        {
            this.TryClose();
        }



        private void RefreshGraph()
        {
            if (_graphableSensor != null && _graphableSensor.DataPoints != null)
                _graphableSensor.RefreshDataPoints();
            UpdateGraph();
        }

        private void UpdateGraph()
        {
            if (SelectedSensor != null)
                SampleValues(Common.MaximumGraphablePoints, _graphableSensor);
            else
                ChartSeries = new List<LineSeries>();
            NotifyOfPropertyChange(() => ChartTitle);
            NotifyOfPropertyChange(() => YAxisTitle);
        }

        private void SampleValues(int numberOfPoints, GraphableSensor sensor)
        {
            var generatedSeries = new List<LineSeries>();

            HideBackground();

            _sampleRate = sensor.DataPoints.Count() / (numberOfPoints);
            Debug.Print("Number of points: {0} Max Number {1} Sampling rate {2}", sensor.DataPoints.Count(), numberOfPoints, _sampleRate);

            var series = (_sampleRate > 1) ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints);
            generatedSeries.Add(new LineSeries { DataSeries = series, LineStroke = new SolidColorBrush(sensor.Colour) });
            if (_sampleRate > 1) ShowBackground();

            var upperLimit = (_sampleRate > 1 && StdDevMode) ? new DataSeries<DateTime, float>("Upper Limit", sensor.UpperLine.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>("Upper Limit", sensor.UpperLine);
            generatedSeries.Add(new LineSeries { DataSeries = upperLimit, LineStroke = Brushes.OrangeRed });           
            var lowerLimit = (_sampleRate > 1 && StdDevMode) ? new DataSeries<DateTime, float>("Lower Limit", sensor.LowerLine.Where((x, index) => index % _sampleRate == 0)) : new DataSeries<DateTime, float>("Lower Limit", sensor.LowerLine);
            generatedSeries.Add(new LineSeries { DataSeries = lowerLimit, LineStroke = Brushes.OrangeRed });
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
                SampleValues(Common.MaximumGraphablePoints, _graphableSensor);
        }

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
            Outliers = (_minMaxMode)
                           ? _sensor.CurrentState.GetOutliersFromMaxAndMin(_ds.DataInterval, _ds.StartTimeStamp,
                                                                           _ds.EndTimeStamp,
                                                                           _sensor.UpperLimit, _sensor.LowerLimit,
                                                                           _sensor.MaxRateOfChange)
                           : _sensor.CurrentState.GetOutliersFromStdDev(_ds.DataInterval, _ds.StartTimeStamp,
                                                                        _ds.EndTimeStamp, NumStdDev, _smoothingPeriod);
            NotifyOfPropertyChange(() => Outliers);
            NotifyOfPropertyChange(() => OutliersStrings);

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
