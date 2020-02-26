using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace WPFLab {
    public abstract class LabApplication : Application {
        readonly IoCService service;
        protected LabApplication() {
            service = new IoCService();
        }

        protected sealed override void OnStartup(StartupEventArgs e) {
            ConfigureServices(service);
            service.Build();
            base.OnStartup(e);
            AppStartup(e, service);
        }

        protected abstract void ConfigureServices(IDependencyRegistrator registrator);
        protected abstract void AppStartup(StartupEventArgs e, IDependencyResolver resolver);
    }
}
