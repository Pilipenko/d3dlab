using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WPFLab {
    public interface IDependencyResolver {
        T Resolve<T>() where T : class;
    }
    public interface IDependencyRegistrator {
        IDependencyRegistrator Register<T>() where T : class;
        IDependencyRegistrator Register<T>(Func<IServiceProvider, T> implementationFactory) where T : class;
    }

    class IoCService : IDependencyRegistrator, IDependencyResolver {
        readonly ServiceCollection service;
        ServiceProvider container;

        public bool IsBuilt { get; internal set; }

        public IoCService() {
            service = new ServiceCollection();

            IsBuilt = false;
        }

        public void Build() {
            Register<IDependencyResolver>(x => this);
            container = service.BuildServiceProvider();
            IsBuilt = true;
        }

        public T Resolve<T>() where T : class {
            if (!IsBuilt) throw new Exception("Service provider is not built.");
            return container.GetService<T>();
        }

        public IDependencyRegistrator Register<T>() where T : class {
            if (IsBuilt) throw new Exception("Can't register new instance, service provider is already built.");
            service.AddScoped<T>();
            IsBuilt = false;
            return this;
        }
        public IDependencyRegistrator Register<T>(Func<IServiceProvider, T> implementationFactory) where T : class {
            if (IsBuilt) throw new Exception("Can't register new instance, service provider is already built.");
            service.AddScoped<T>(implementationFactory);
            IsBuilt = false;
            return this;
        }

        public IDependencyRegistrator RegisterAsSingleton<T>() where T : class {
            if (IsBuilt) throw new Exception("Can't register new instance, service provider is already built.");
            service.AddSingleton<T>();
            IsBuilt = false;
            return this;
        }
        public IDependencyRegistrator RegisterAsSingleton<T>(Func<IServiceProvider, T> implementationFactory) where T : class {
            if (IsBuilt) throw new Exception("Can't register new instance, service provider is already built.");
            service.AddSingleton<T>(implementationFactory);
            IsBuilt = false;
            return this;
        }
    }
}
