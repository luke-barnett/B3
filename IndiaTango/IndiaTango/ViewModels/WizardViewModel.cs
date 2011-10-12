using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class WizardViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _manager;
        private int _currentStep;
        private Dataset _ds;

        public WizardViewModel(SimpleContainer container, IWindowManager manager)
        {
            _container = container;
            _manager = manager;
        }

        public List<Sensor> Sensors
        {
            get { return _ds.Sensors; }
        }

        public Dataset Dataset
        {
            get { return _ds; }
            set { _ds = value; NotifyOfPropertyChange(() => Sensors); }
        }

        public int CurrentStep
        {
            get { return _currentStep; }
            set { _currentStep = value; NotifyOfPropertyChange(() => CurrentStep); NotifyOfPropertyChange(() => CanGoBack); NotifyOfPropertyChange(() => CanGoForward); }
        }

        public string Title
        {
            get { return "Import Wizard"; }
        }

        public string WizardTitle
        {
            get { return "Fix imported data" + ((SelectedSensor == null) ? "" : " for " + SelectedSensor.Name); }
        }

        private int _currentSensorIndex = 0;

        public void BtnNext()
        {
            if (CurrentStep == 2)
            {
                if (_currentSensorIndex >= Sensors.Count - 1)
                {
                    SelectedSensor = null;
                    CurrentStep = 3;
                }
                else
                {
                    SelectedSensor = Sensors[++_currentSensorIndex];
                    CurrentStep = 1;
                }
            }
            else
            {
                SelectedSensor = Sensors[_currentSensorIndex];
                CurrentStep++;
            }
        }

        public void BtnBack()
        {
            if (CurrentStep == 1)
            {
                if (_currentSensorIndex < 1)
                {
                    SelectedSensor = null;
                    CurrentStep = 0;
                }
                else
                {
                    SelectedSensor = Sensors[--_currentSensorIndex];
                    CurrentStep = 2;
                }
            }
            else
            {
                SelectedSensor = Sensors[_currentSensorIndex];
                CurrentStep--;
            }
        }

        public bool CanGoBack
        {
            get { return CurrentStep > 0; }
        }

        public bool CanGoForward
        {
            get { return CurrentStep < 3; }
        }

        public void BtnFinish()
        {
            TryClose();
        }

        public List<Grid> SensorsToRename
        {
            get
            {
                var list = new List<Grid>();

                for (var i = 0; i < Sensors.Count; i++)
                {
                    var item = new Grid
                                   {
                                       Background =
                                           i % 2 == 0
                                               ? new SolidColorBrush(Color.FromArgb(180, 240, 240, 240))
                                               : Brushes.White
                                   };
                    item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var textbox = new TextBox
                                      {
                                          Text = Sensors[i].GuessConventionalNameForSensor() + "_",
                                          Background = Brushes.Transparent
                                      };
                    Grid.SetColumn(textbox, 1);
                    item.Children.Add(textbox);
                    var currentSensor = Sensors[i];
                    textbox.TextChanged += (o, e) =>
                                               {
                                                   Debug.WriteLine("Fired for " + currentSensor.Name);
                                                   currentSensor.Name = textbox.Text;
                                               };

                    var textblock = new TextBlock { Text = Sensors[i].Name };
                    Grid.SetColumn(textblock, 0);
                    item.Children.Add(textblock);

                    list.Add(item);
                }

                return list;
            }
        }

        public void BtnStdDev()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.DataSet = _ds;
            erroneousValuesView.OnlyUseStandarDeviation();
            _manager.ShowWindow(erroneousValuesView);
        }

        public void BtnMissingValues()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.DataSet = _ds;
            erroneousValuesView.OnlyUseMissingValues();
            _manager.ShowWindow(erroneousValuesView);
        }

        public void BtnOutliers()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.DataSet = _ds;
            erroneousValuesView.OnlyUseMinMaxRateOfChange();
            _manager.ShowWindow(erroneousValuesView);
        }

        public void BtnCalibrate()
        {
            var calibrateView = (_container.GetInstance(typeof(CalibrateSensorsViewModel), "CalibrateSensorsViewModel") as CalibrateSensorsViewModel);
            if(calibrateView == null)
                return;
            calibrateView.Dataset = _ds;
            _manager.ShowWindow(calibrateView);
        }

        public int ErrorRowHeight
        {
            get { return (FailingErrorVisible) ? 60 : 0; }
        }

        private bool _errorVisible;

        public bool FailingErrorVisible
        {
            get { return _errorVisible; }
            set
            {
                _errorVisible = value;

                NotifyOfPropertyChange(() => FailingErrorVisible);
                NotifyOfPropertyChange(() => ErrorRowHeight);
            }
        }

        private Sensor _selectedSensor;
        public Sensor SelectedSensor
        {
            get { return _selectedSensor; }
            set
            {
                _selectedSensor = value;

                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => Description);
                NotifyOfPropertyChange(() => LowerLimit);
                NotifyOfPropertyChange(() => UpperLimit);
                NotifyOfPropertyChange(() => Unit);
                NotifyOfPropertyChange(() => MaximumRateOfChange);
                NotifyOfPropertyChange(() => Manufacturer);
                NotifyOfPropertyChange(() => SerialNumber);
                NotifyOfPropertyChange(() => ErrorThreshold);
                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => WizardTitle);
            }
        }

        public string Name { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Name; } set { if (SelectedSensor != null) SelectedSensor.Name = value; } }
        public string Description { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Description; } set { if (SelectedSensor != null) SelectedSensor.Description = value; } }
        public string LowerLimit
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.LowerLimit.ToString(); }
            set
            {
                float val;

                if (SelectedSensor != null && float.TryParse(value, out val))
                    SelectedSensor.LowerLimit = val;
            }
        }
        public string UpperLimit
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.UpperLimit.ToString(); }
            set
            {
                float val;

                if (SelectedSensor != null && float.TryParse(value, out val))
                    SelectedSensor.UpperLimit = val;
            }
        }
        public string Unit { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Unit; } set { if (SelectedSensor != null) SelectedSensor.Unit = value; } }
        public string MaximumRateOfChange
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.MaxRateOfChange.ToString(); }
            set
            {
                float val;

                if (SelectedSensor != null && float.TryParse(value, out val))
                    SelectedSensor.MaxRateOfChange = val;
            }
        }
        public string Manufacturer { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Manufacturer; } set { if (SelectedSensor != null) SelectedSensor.Manufacturer = value; } }
        public string SerialNumber { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.SerialNumber; } set { if (SelectedSensor != null) SelectedSensor.SerialNumber = value; } }
        public string ErrorThreshold
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.ErrorThreshold.ToString(); }
            set
            {
                int val;

                if (SelectedSensor != null && int.TryParse(value, out val))
                    SelectedSensor.ErrorThreshold = val;
            }
        }

        public int SummaryType
        {
            get { return (SelectedSensor == null) ? 0 : (int)SelectedSensor.SummaryType; }
            set
            {
                if (SelectedSensor != null)
                    SelectedSensor.SummaryType = (SummaryType)value;
                NotifyOfPropertyChange(() => SummaryType);
            }
        }

        public string[] SummaryTypes { get { return new[] { "Average", "Sum" }; } }
    }
}
