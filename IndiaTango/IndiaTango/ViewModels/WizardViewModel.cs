using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class WizardViewModel : BaseViewModel
    {
        private SimpleContainer _container;
        private IWindowManager _manager;
        private int _currentStep = 0;
        private Dataset _ds = null;

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
            set { _ds = value; NotifyOfPropertyChange(() => Sensors);}
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
            get { return "Fix imported data"; }
        }

        public void btnNext()
        {
            CurrentStep = Math.Min(++CurrentStep, 4);
        }

        public void btnBack()
        {
            CurrentStep = Math.Max(--CurrentStep, 0);
        }

        public bool CanGoBack
        {
            get { return CurrentStep > 0; }
        }

        public bool CanGoForward
        {
            get { return CurrentStep < 4; }
        }

        public void btnFinish()
        {
            this.TryClose();
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
                    item.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    item.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

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

        public void btnStdDev()
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

        public void btnMissingValues()
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

        public void btnOutliers()
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

        public void btnCalibrate()
        {
            var calibrateView = (_container.GetInstance(typeof(CalibrateSensorsViewModel), "CalibrateSensorsViewModel") as CalibrateSensorsViewModel);
            calibrateView.Dataset = _ds;
            _manager.ShowWindow(calibrateView);
        }
    }
}
