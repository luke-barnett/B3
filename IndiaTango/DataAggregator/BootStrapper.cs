using System;
using System.Collections.Generic;
using Caliburn.Micro;
using DataAggregator.ViewModels;

namespace DataAggregator
{
    public class BootStrapper : Bootstrapper
    {
        private SimpleContainer _container;

        protected override void Configure()
        {
            _container = new SimpleContainer();

            _container.RegisterSingleton(typeof(MainWindowViewModel), "MainWindowViewModel", typeof(MainWindowViewModel));

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
            var windowManager = _container.GetInstance(typeof (IWindowManager), null) as IWindowManager;
            if (windowManager != null)
                windowManager.ShowWindow(_container.GetInstance(typeof(MainWindowViewModel), "MainWindowViewModel"));
        }
    }
}
