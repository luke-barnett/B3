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

            _container.RegisterSingleton(typeof(WindowManager), "WindowManager", typeof(WindowManager));
            _container.RegisterSingleton(typeof(MainViewModel), "MainViewModel", typeof(MainViewModel));

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
            (_container.GetInstance(typeof(WindowManager), "WindowManager") as WindowManager).ShowWindow(_container.GetInstance(typeof(MainViewModel), "MainViewModel"));
        }
    }
}
