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
        private int _numStdDev;
        private int _smoothingPeriod;
		private Cursor _viewCursor = Cursors.Arrow;

        private List<LineSeries> _chartSeries = new List<LineSeries>();
        private BehaviourManager _behaviour;
        private DoubleRange _range;
        private int _sampleRate;
        private GraphableSensor _graphableSensor;
        private Canvas _backgroundCanvas;

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
        }

        #region View Properties

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
                                                                            _ds.EndTimeStamp, NumStdDev, SmoothingPeriod);
                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => OutliersStrings);
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

        public int NumStdDev
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
            get { return _smoothingPeriod/(60/_ds.DataInterval); }
            set
            {
                
                _smoothingPeriod= value*(60/_ds.DataInterval);
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
			get { return SelectedSensor != null && SelectedSensor.UndoStates.Count > 1; }
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
            EventLogger.LogInfo(GetType().ToString(), "Value removal started.");
            if (_selectedValues.Count == 0)
                return;

        	ViewCursor = Cursors.Wait;
            _sensor.AddState(_sensor.CurrentState.removeValues(SelectedValues));

            Finalise("Removed selected values from dataset.");

            RefreshGraph();
            Common.ShowMessageBox("Values Updated", "The selected values have been removed from the data", false, false);

            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        	ViewCursor = Cursors.Arrow;
        }

        public void btnMakeZero()
        {
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

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
            Outliers = (_minMaxMode)
                           ? _sensor.CurrentState.GetOutliersFromMaxAndMin(_ds.DataInterval, _ds.StartTimeStamp,
                                                                           _ds.EndTimeStamp,
                                                                           _sensor.UpperLimit, _sensor.LowerLimit,
                                                                           _sensor.MaxRateOfChange)
                           : _sensor.CurrentState.GetOutliersFromStdDev(_ds.DataInterval, _ds.StartTimeStamp,
                                                                        _ds.EndTimeStamp,NumStdDev,SmoothingPeriod);
            NotifyOfPropertyChange(() => Outliers);
            NotifyOfPropertyChange(() => OutliersStrings);

            Common.requestReason(_sensor, _container, _windowManager, _sensor.CurrentState, taskPerformed);
        }

        public void btnSpecify()
        {
            //TODO refactor
            EventLogger.LogInfo(GetType().ToString(), "Value updation started.");

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
                catch (FormatException f)
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
            Outliers = (_minMaxMode)
                           ? _sensor.CurrentState.GetOutliersFromMaxAndMin(_ds.DataInterval, _ds.StartTimeStamp,
                                                                           _ds.EndTimeStamp,
                                                                           _sensor.UpperLimit, _sensor.LowerLimit,
                                                                           _sensor.MaxRateOfChange)
                           : _sensor.CurrentState.GetOutliersFromStdDev(_ds.DataInterval, _ds.StartTimeStamp,
                                                                        _ds.EndTimeStamp,NumStdDev,SmoothingPeriod);
            RefreshGraph();
			NotifyOfPropertyChange(() => UndoButtonEnabled);
			NotifyOfPropertyChange(() => RedoButtonEnabled);
        }

        public void btnRedo()
        {
            _sensor.Redo();
            Outliers = (_minMaxMode)
                           ? _sensor.CurrentState.GetOutliersFromMaxAndMin(_ds.DataInterval, _ds.StartTimeStamp,
                                                                           _ds.EndTimeStamp,
                                                                           _sensor.UpperLimit, _sensor.LowerLimit,
                                                                           _sensor.MaxRateOfChange)
                           : _sensor.CurrentState.GetOutliersFromStdDev(_ds.DataInterval, _ds.StartTimeStamp,
                                                                        _ds.EndTimeStamp, NumStdDev, SmoothingPeriod);
            RefreshGraph();
			NotifyOfPropertyChange(() => UndoButtonEnabled);
			NotifyOfPropertyChange(() => RedoButtonEnabled);
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

            generatedSeries.Add(new LineSeries { DataSeries = new DataSeries<DateTime, float>("Upper Limit") { new DataPoint<DateTime, float>((sensor.BoundsSet) ? sensor.LowerBound : sensor.Sensor.Owner.StartTimeStamp, sensor.Sensor.UpperLimit), new DataPoint<DateTime, float>((sensor.BoundsSet) ? sensor.UpperBound : sensor.Sensor.Owner.EndTimeStamp, sensor.Sensor.UpperLimit) }, LineStroke = Brushes.OrangeRed });
            generatedSeries.Add(new LineSeries { DataSeries = new DataSeries<DateTime, float>("Lower Limit") { new DataPoint<DateTime, float>((sensor.BoundsSet) ? sensor.LowerBound : sensor.Sensor.Owner.StartTimeStamp, sensor.Sensor.LowerLimit), new DataPoint<DateTime, float>((sensor.BoundsSet) ? sensor.UpperBound : sensor.Sensor.Owner.EndTimeStamp, sensor.Sensor.LowerLimit) }, LineStroke = Brushes.OrangeRed });

            ChartSeries = generatedSeries;
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

    }
}
