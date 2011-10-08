using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class SensorTemplateManagerViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        
        private SensorTemplate _selectedTemplate = null;
        private Visibility _editDoneVisible = Visibility.Visible;
        private Visibility _saveCancelVisible = Visibility.Collapsed;
        private bool _editing = false;
        private bool _listEnabled = true;
        private List<SensorTemplate> _allTemplates = new List<SensorTemplate>();
        private List<ListedSensor> _sensors; 

        private string _unit = "";
        private float _lowerLimit = 0;
        private float _upperLimit = 0;
        private float _maxChange = 0;
        private string _pattern = "";
        private int _selectedMatchMode = 0;
        private SummaryType _sType;

        public Dataset Dataset { get; set; }

        public SensorTemplateManagerViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            AllTemplates = SensorTemplate.ImportAll();
        }

        public List<SensorTemplate> AllTemplates
        {
            get { return _allTemplates; }
            set { _allTemplates = value; NotifyOfPropertyChange(() => AllTemplates); }
        }

        public string Title { get { return string.Format("[{0}] Sensor Presets", (Dataset != null ? Dataset.IdentifiableName : Common.UnknownSite)); } }
        
        public string[] SensorMatchStyles
        {
            get
            {
                return
                    new string[]
                        {
                            "If they contain the text to match", "If they start with the text to match", "If they end with the text to match"
                        };
            }
        }

        public int SummaryMode
        {
            get { return (int)_sType; }
            set
            {
                _sType = (SummaryType) value;
                NotifyOfPropertyChange(() => SummaryMode);
            }
        }

        public string[] SummaryTypes
        {
            get{return new string[]{"Average","Sum"};}
        }

        public List<ListedSensor> Sensors
        {
            get { return _sensors; }
            set { _sensors = value; }
        } 

        public string Unit
        {
            get { return _unit; }
            set { _unit = value; NotifyOfPropertyChange(() => Unit);
            }
        }

        public float UpperLimit
        {
            get { return _upperLimit; }
            set { _upperLimit = value; NotifyOfPropertyChange(() => UpperLimit);
            }
        }

        public float LowerLimit
        {
            get { return _lowerLimit; }
            set { _lowerLimit = value; NotifyOfPropertyChange(() => LowerLimit);
            }
        }

        public float MaximumRateOfChange
        {
            get { return _maxChange; }
            set { _maxChange = value; NotifyOfPropertyChange(() => MaximumRateOfChange);
            }
        }

        public string TextToMatch
        {
            get { return _pattern; }
            set { _pattern = value; NotifyOfPropertyChange(() => TextToMatch); }
        }

        public int MatchMode { get { return _selectedMatchMode; } set { _selectedMatchMode = value; NotifyOfPropertyChange(() => MatchMode); } }

        public Visibility EditDoneVisible
        {
            get { return _editDoneVisible; }
            set
            { 
                _editDoneVisible = value;
                NotifyOfPropertyChange(() => EditDoneVisible); 
                NotifyOfPropertyChange(() => SaveCancelVisible);
            }
        }

        public Visibility SaveCancelVisible
        {
            get { return _saveCancelVisible; }
            set
            {
                _saveCancelVisible = value;
                NotifyOfPropertyChange(() => SaveCancelVisible);
                NotifyOfPropertyChange(() => EditDoneVisible);
            }
        }

        public bool ListEnabled
        {
            get { return _listEnabled; }
            set { _listEnabled = value; NotifyOfPropertyChange(() => ListEnabled); }
        }

        public bool Editing
        {
            get { return _editing; }
            set
            { 
                _editing = value;
                ListEnabled = !_editing;

                SaveCancelVisible = (value) ? Visibility.Visible : Visibility.Collapsed;
                EditDoneVisible = (value) ? Visibility.Collapsed : Visibility.Visible;

                NotifyOfPropertyChange(() => Editing);
                NotifyOfPropertyChange(() => SaveCancelVisible);
                NotifyOfPropertyChange(() => EditDoneVisible);
            }
        }

        public SensorTemplate SelectedTemplate
        {
            get { return _selectedTemplate; }
            set
            {
                _selectedTemplate = value;

                if(value == null)
                {
                    Unit = "";
                    LowerLimit = 0;
                    UpperLimit = 0;
                    TextToMatch = "";
                    MatchMode = 0;
                    SummaryMode = (int)SummaryType.Average;
                }
                else
                {
                    Unit = _selectedTemplate.Unit;
                    UpperLimit = _selectedTemplate.UpperLimit;
                    LowerLimit = _selectedTemplate.LowerLimit;
                    TextToMatch = _selectedTemplate.Pattern;
                    MatchMode = (int) _selectedTemplate.MatchingStyle;
                    SummaryMode = (int)_selectedTemplate.SummaryType;
                }

                NotifyOfPropertyChange(() => SelectedTemplate);
                NotifyOfPropertyChange(() => HasSelectedTemplate);
            }
        }

        public bool HasSelectedTemplate
        {
            get { return SelectedTemplate != null; }
        }

        public void btnDone()
        {
            this.TryClose();
        }

        public void btnEdit()
        {
            Editing = true;
        }

        public void btnCancel()
        {
            Editing = false;
        }

        public void btnNew()
        {
            SelectedTemplate = null;
            Editing = true;
        }

        public void btnSave()
        {
            try
            {
                var msg = "";
                var template = new SensorTemplate(Unit, UpperLimit, LowerLimit, MaximumRateOfChange, (SensorTemplate.MatchStyle)Enum.Parse(typeof(SensorTemplate.MatchStyle), (MatchMode).ToString()), TextToMatch,_sType); // Construct object to prevent inconsistent state if updating (when setting properties and some are invalid)

                var list = AllTemplates; // To trigger update

                if(SelectedTemplate == null)
                {
                    // New
                    list.Add(template);
                    AllTemplates = list;

                    foreach (var sensor in _sensors)
                    {
                        foreach (var sensorTemplate in list)
                            if(sensorTemplate.Matches(sensor.Sensor))
                                sensorTemplate.ProvideDefaultValues(sensor.Sensor);
                    }

                    msg = "Sensor preset successfully created.";
                }
                else
                {
                    // Update
                    list[list.IndexOf(SelectedTemplate)] = template;
                    SelectedTemplate = template;
                    AllTemplates = list;

                    foreach (var sensor in _sensors)
                    {
                        foreach (var sensorTemplate in list)
                            if(sensorTemplate.Matches(sensor.Sensor))
                                sensorTemplate.ProvideDefaultValues(sensor.Sensor);
                    }

                    msg = "Sensor preset successfully updated.";
                }

                SensorTemplate.ExportAll(AllTemplates);

                Common.ShowMessageBox("Presets", msg, false, false);

                Editing = false;
                SelectedTemplate = null;

                AllTemplates = new List<SensorTemplate>(); // To force update!
                AllTemplates = list;
            }
            catch (Exception e)
            {
                Common.ShowMessageBox("Error", e.Message, false, true);
            }
        }

        public void btnDelete()
        {
            if(SelectedTemplate != null && Common.Confirm("Delete Preset", "Are you sure you want to permanently delete this preset?"))
            {
                var list = AllTemplates;
                list.Remove(SelectedTemplate);

                SensorTemplate.ExportAll(list);

                AllTemplates = new List<SensorTemplate>();
                AllTemplates = list;

                Common.ShowMessageBox("Delete Preset", "Preset successfully deleted.", false, false);
            }
        }
    }
}
