using System.Collections.Generic;
using Caliburn.Micro;
using IndiaTango.ViewModels;
using System;

namespace IndiaTango
{
    public class IndiaTangoBootstrapper : Bootstrapper
    {
        private SimpleContainer _container;

        protected override void Configure()
        {
            _container = new SimpleContainer();

            _container.RegisterSingleton(typeof(MainViewModel), "MainViewModel", typeof(MainViewModel));
            _container.RegisterPerRequest(typeof(GraphViewModel), "GraphViewModel", typeof(GraphViewModel));
            _container.RegisterPerRequest(typeof(SessionViewModel), "SessionViewModel", typeof(SessionViewModel));
            _container.RegisterPerRequest(typeof(BuoyDetailsViewModel), "BuoyDetailsViewModel", typeof(BuoyDetailsViewModel));
            _container.RegisterPerRequest(typeof(MissingValuesViewModel), "MissingValuesViewModel", typeof(MissingValuesViewModel));
            _container.RegisterPerRequest(typeof(ContactEditorViewModel), "ContactEditorViewModel", typeof(ContactEditorViewModel));
            _container.RegisterPerRequest(typeof(EditSensorViewModel), "EditSensorViewModel", typeof(EditSensorViewModel));
            _container.RegisterPerRequest(typeof(SpecifyValueViewModel), "SpecifyValueViewModel", typeof(SpecifyValueViewModel));
            _container.RegisterPerRequest(typeof(SensorTemplateManagerViewModel), "SensorTemplateManagerViewModel", typeof(SensorTemplateManagerViewModel));
            _container.RegisterPerRequest(typeof(ExportViewModel), "ExportViewModel", typeof(ExportViewModel));
            _container.RegisterPerRequest(typeof(OutlierDetectionViewModel), "OutlierDetectionViewModel", typeof(OutlierDetectionViewModel));
            _container.RegisterPerRequest(typeof(CalibrateSensorsViewModel), "CalibrateSensorsViewModel", typeof(CalibrateSensorsViewModel));
            _container.RegisterPerRequest(typeof(SettingsViewModel), "SettingsViewModel", typeof(SettingsViewModel));
            _container.RegisterSingleton(typeof(LogWindowViewModel), "LogWindowViewModel", typeof(LogWindowViewModel));
            _container.RegisterPerRequest(typeof(ExportToImageViewModel), "ExportToImageViewModel", typeof(ExportToImageViewModel));
            _container.RegisterPerRequest(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel", typeof(ErroneousValuesDetectionViewModel));
            _container.RegisterPerRequest(typeof(UseSelectedRangeViewModel), "UseSelectedRangeViewModel", typeof(UseSelectedRangeViewModel));
            _container.RegisterPerRequest(typeof(WizardViewModel), "WizardViewModel", typeof(WizardViewModel));
            _container.RegisterSingleton(typeof(MainWindowViewModel), "MainWindowViewModel", typeof(MainWindowViewModel));
            _container.RegisterPerRequest(typeof(EditSiteDataViewModel), "EditSiteDataViewModel", typeof(EditSiteDataViewModel));
            _container.RegisterPerRequest(typeof(MatchToExistingSensorsViewModel), "MatchToExistingSensorsViewModel", typeof(MatchToExistingSensorsViewModel));

            _container.RegisterInstance(typeof(IWindowManager), null, new WindowManager());
            _container.RegisterInstance(typeof(SimpleContainer), null, _container);
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            //(_container.GetInstance(typeof(IWindowManager), null) as IWindowManager).ShowWindow(_container.GetInstance(typeof(MainViewModel), "MainViewModel"));
            (_container.GetInstance(typeof(IWindowManager), null) as IWindowManager).ShowWindow(_container.GetInstance(typeof(MainWindowViewModel), "MainWindowViewModel"));
        }
    }
}
