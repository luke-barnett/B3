using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class EditSensorViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        private Sensor _sensor = null;

        public EditSensorViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public string Icon { get { return Common.Icon; } }

        public string TipVisibleEnum
        {
            get { return (TipVisible) ? "Visible" : "Collapsed"; }
        }

        public int TipRowHeight
        {
            get { return (TipVisible) ? 45 : 0; }
        }

        private bool _tipVisible = false;

        public bool TipVisible
        {
            get { return _tipVisible; }
            set
            {
                _tipVisible = value;
                
                NotifyOfPropertyChange(() => TipVisible);
            }
        }

        public bool NeedsTip
        {
            get { return String.IsNullOrEmpty(Unit); }
        }

        public Sensor ActiveSensor
        {
            get { return _sensor; }
            set
            {
                _sensor = value; 

                if(_sensor != null)
                {
                    Name = _sensor.Name;
                    Description = _sensor.Description;
                    LowerLimit = _sensor.LowerLimit.ToString();
                    UpperLimit = _sensor.UpperLimit.ToString();
                    Unit = _sensor.Unit;
                    MaximumRateOfChange = _sensor.MaxRateOfChange.ToString();
                    Manufacturer = _sensor.Manufacturer;
                }
                else
                {
                    Name = "";
                    Description = "";
                    LowerLimit = "0";
                    UpperLimit = "0";
                    Unit = "";
                    MaximumRateOfChange = "0";
                    Manufacturer = "";
                }

                NotifyOfPropertyChange(() => NeedsTip);

                if(NeedsTip)
                {
                    // Provide defaults based on name
                    if(Name.Contains("Temp"))
                    {
                        // TODO: load these from presets on disk
                        Unit = "°C";
                        LowerLimit = "-15";
                        UpperLimit = "38";
                        MaximumRateOfChange = "9";
                        TipVisible = true;
                    }
                }
            }
        }

        public string Title { get { return "Edit Sensor"; } }

        public string Name { get; set; }
        public string Description { get; set; }
        public string LowerLimit { get; set; }
        public string UpperLimit { get; set; }
        public string Unit { get; set; }
        public string MaximumRateOfChange { get; set; }
        public string Manufacturer { get; set; }

        public void btnCancel()
        {
            this.TryClose();
        }

        public void btnSave()
        {
            if(ActiveSensor == null)
            {
                // New sensor
                try
                {
                    // TODO: more user-friendly conversion messages!
                    Sensor s = new Sensor(Name, Description, float.Parse(UpperLimit), float.Parse(LowerLimit), Unit, float.Parse(MaximumRateOfChange), Manufacturer);
                    ActiveSensor = s;
                    this.TryClose();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Existing sensor
                try
                {
                    ActiveSensor.Name = Name;
                    ActiveSensor.Description = Description;
                    ActiveSensor.UpperLimit = float.Parse(UpperLimit);
                    ActiveSensor.LowerLimit = float.Parse(LowerLimit);
                    ActiveSensor.Unit = Unit;
                    ActiveSensor.MaxRateOfChange = float.Parse(MaximumRateOfChange);
                    ActiveSensor.Manufacturer = Manufacturer;

                    this.TryClose();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
