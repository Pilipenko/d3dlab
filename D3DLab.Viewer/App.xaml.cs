using D3DLab.Viewer.Presentation;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using WPFLab;

namespace D3DLab.Viewer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class LabApp : LabApplication {
        public LabApp() {
            Syncfusion.Licensing.SyncfusionLicenseProvider
                .RegisterLicense(Viewer.Properties.Resources.ResourceManager.GetString("SyncfusionLicense"));

            Syncfusion.SfSkinManager.SfSkinManager.ApplyStylesOnApplication = true;
        }

        protected override void ConfigureServices(IDependencyRegistrator registrator) {
            registrator
                .RegisterApplication(this)
                .RegisterView<MainWindow, MainWindowViewModel>()
                .RegisterMvvm()
                ;
        }

        protected override void AppStartup(StartupEventArgs e, IDependencyResolver resolver) {
            resolver.ResolveView<MainWindow, MainWindowViewModel>().Show();
        }

        
    }
}
