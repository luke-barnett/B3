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

            _container.RegisterPerRequest(typeof(ContactEditorViewModel), "ContactEditorViewModel", typeof(ContactEditorViewModel));
            _container.RegisterPerRequest(typeof(EditSensorViewModel), "EditSensorViewModel", typeof(EditSensorViewModel));
            _container.RegisterPerRequest(typeof(SpecifyValueViewModel), "SpecifyValueViewModel", typeof(SpecifyValueViewModel));
            _container.RegisterPerRequest(typeof(SensorTemplateManagerViewModel), "SensorTemplateManagerViewModel", typeof(SensorTemplateManagerViewModel));
            _container.RegisterPerRequest(typeof(ExportViewModel), "ExportViewModel", typeof(ExportViewModel));
            _container.RegisterPerRequest(typeof(SettingsViewModel), "SettingsViewModel", typeof(SettingsViewModel));
            _container.RegisterSingleton(typeof(LogWindowViewModel), "LogWindowViewModel", typeof(LogWindowViewModel));
            _container.RegisterPerRequest(typeof(ExportToImageViewModel), "ExportToImageViewModel", typeof(ExportToImageViewModel));
            _container.RegisterPerRequest(typeof(UseSelectedRangeViewModel), "UseSelectedRangeViewModel", typeof(UseSelectedRangeViewModel));
            _container.RegisterSingleton(typeof(MainWindowViewModel), "MainWindowViewModel", typeof(MainWindowViewModel));
            _container.RegisterPerRequest(typeof(EditSiteDataViewModel), "EditSiteDataViewModel", typeof(EditSiteDataViewModel));
            _container.RegisterPerRequest(typeof(MatchToExistingSensorsViewModel), "MatchToExistingSensorsViewModel", typeof(MatchToExistingSensorsViewModel));
            _container.RegisterPerRequest(typeof(CalibrationDetailsViewModel), "CalibrationDetailsViewModel", typeof(CalibrationDetailsViewModel));
            _container.RegisterPerRequest(typeof(HeatMapViewModel), "HeatMapViewModel", typeof(HeatMapViewModel));
            _container.RegisterSingleton(typeof(AboutViewModel), "AboutViewModel", typeof(AboutViewModel));

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
