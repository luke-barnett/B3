using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;

namespace IndiaTango.ViewModels
{
    class SensorTemplateManagerViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        private int _selectedMatchMode = 0;

        public SensorTemplateManagerViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public string Title { get { return "Manage Sensor Presets"; } }
        public string[] SensorMatchStyles
        {
            get
            {
                return
                    new string[]
                        {
                            "If they start with the text to match", "If they end with the text to match",
                            "If they contain the text to match"
                        };
            }
        }

        public string Unit { get; set; }
        public float UpperLimit { get; set; }
        public float LowerLimit { get; set; }
        public float MaximumRateOfChange { get; set; }
        public string TextToMatch { get; set; }
        public int MatchMode { get { return _selectedMatchMode; } set { _selectedMatchMode = value; } }
    }
}
