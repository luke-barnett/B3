using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    internal class CalibrateSensorsViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private Sensor _sensor;
    	private int _zoomLevel = 100;
        private Dataset _ds;
        private string _formulaText = "";
        private bool _validFormula = true;
        private DateTime _startDateTime, _endDateTime;
        private FormulaEvaluator _eval;
        private CompilerResults _results;

        public CalibrateSensorsViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _container = container;
            _windowManager = windowManager;
            _eval = new FormulaEvaluator();
        }

		#region View Properties

        public Brush FormulaBoxBackground
        {
            get { return ValidFormula ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(126,255, 69, 0)); }
        }

        public string FormulaText
        {
            get { return _formulaText; }
            set
            {
                _formulaText = value;

                 _results = _eval.ParseFormula(value);
                
                _validFormula = _results != null && _results.CompiledAssembly != null;

                NotifyOfPropertyChange(() => FormulaText);
                NotifyOfPropertyChange(() => ValidFormula);
            }
        }
		
        public bool ValidFormula
        {
            get { return _validFormula; }
            set { _validFormula = value; NotifyOfPropertyChange(() => ValidFormula); NotifyOfPropertyChange(() => FormulaBoxBackground);}
        }

        public bool RedoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.RedoStack.Count > 0; }
        }

        public bool UndoButtonEnabled
        {
            get { return SelectedSensor != null && SelectedSensor.UndoStack.Count > 1; }
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

        public Dataset Dataset { get { return _ds; } set { _ds = value; } }

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

				NotifyOfPropertyChange(() => SelectedSensor);
				NotifyOfPropertyChange(() => SensorName);
                NotifyOfPropertyChange(() => UndoButtonEnabled);
                NotifyOfPropertyChange(() => RedoButtonEnabled);
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => CanEditDates);
			}
		}


        public DateTime StartTime
        {
            get { return _startDateTime; } 
            set { _startDateTime = value; NotifyOfPropertyChange(() => StartTime); NotifyOfPropertyChange(() => CanEditDates); }
        }

        public DateTime EndTime 
        { 
            get { return _endDateTime; } 
            set { _endDateTime = value; NotifyOfPropertyChange(() => EndTime); NotifyOfPropertyChange(() => CanEditDates); } 
        }

        public bool CanEditDates
        {
            get { return (SelectedSensor != null); }
        }

		#endregion

		#region Event Handlers

        public void StartTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            //TODO: change date range on graph
        }

        public void EndTimeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            //TODO: change date range on graph
        }

		public void btnUndo()
		{
            _sensor.Undo();
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
            //TODO: Update graph
            
		}

		public void btnRedo()
		{
            _sensor.Redo();
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
            //TODO: Update graph
		}

		public void btnDone()
		{
			this.TryClose();
		}

        public void btnHelp()
        {
            //TODO: Help dialog
            //Common.ShowFeatureNotImplementedMessageBox();
            FormulaText = FormulaText;
        }

        public void btnApply()
        {
            Common.ShowFeatureNotImplementedMessageBox();
            NotifyOfPropertyChange(() => UndoButtonEnabled);
            NotifyOfPropertyChange(() => RedoButtonEnabled);
        }

        public void btnClear()
        {
            FormulaText = "";
        }

        public void requestReason()
        {
            if(_sensor != null && _sensor.CurrentState != null)
            {
                var specify = (SpecifyValueViewModel)_container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel");
                specify.Title = "Log Reason";
                specify.Message = "Please specify a reason for this change:";
                specify.Deactivated += (o, e) =>
                                           {
                                               // Specify reason
                                               _sensor.CurrentState.Reason = specify.Text;
                                           };
                _windowManager.ShowDialog(specify);
            }
        }

		public void btnZoomIn()
		{
			//TODO: Implement zoom
			ZoomLevel += 100;
		}

		public void btnZoomOut()
		{
			ZoomLevel -= 100;
		}

		public void sldZoom(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ZoomLevel = (int)e.NewValue;
		}
		#endregion
    }
}
