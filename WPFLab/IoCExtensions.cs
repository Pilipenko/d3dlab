using System.ComponentModel;
using System.Windows;
using WPFLab.MVVM;

namespace WPFLab {
    public static class IoCExtensions {
        public static IDependencyRegistrator RegisterView<TView,TViewModel>(this IDependencyRegistrator service) 
            where TView : FrameworkElement
            where TViewModel : class, INotifyPropertyChanged {
          
            service.Register<TView>();
            service.Register<TViewModel>();

            return service;
        }
        public static TView ResolveView<TView, TViewModel>(this IDependencyResolver container) 
            where TView : FrameworkElement
            where TViewModel : BaseNotify {
            
            var view = container.Resolve<TView>();
            var vm = container.Resolve<TViewModel>();

            view.DataContext = vm;
            view.Loaded += (x,y) => vm.OnLoaded();
            view.Unloaded += (x,y) => vm.OnUnloaded();

            return view;
        }

        public static IDependencyRegistrator RegisterApplication<T>(this IDependencyRegistrator service, T app) where T : LabApplication {
            service.Register(x => app);
            return service;
        }

        public static IDependencyRegistrator RegisterMvvm(this IDependencyRegistrator service) {

            
            service.Register<IDialogManager>(x => new DialogManager());


            return service;
        }
    }
}
