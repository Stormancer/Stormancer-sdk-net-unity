using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Core.DependencyResolver;
using TinyIoC;

namespace Stormancer
{
    class DefaultDependencyResolver : IDependencyResolver
    {
        private TinyIoC.TinyIoCContainer _container;

        public TinyIoCContainer Container
        {
            get
            {
                return _container;
            }
        }

        internal DefaultDependencyResolver() : this(null)
        {

        }

        internal DefaultDependencyResolver(IDependencyResolver container) : this(container, b => { })
        {

        }

        internal DefaultDependencyResolver(IDependencyResolver container, Action<IDependencyBuilder> builder)
        {
            if (container != null)
            {
                _container = ((DefaultDependencyResolver)container).Container.GetChildContainer();
            }
            else
            {
                _container = new TinyIoCContainer();
            }
            var b = new Core.DependencyResolver.DefaultDependencyBuilder();
            builder(b);

            UpdateContainerFromBuilder(_container, b);
        }



        public T Resolve<T>() where T : class
        {

            return _container.Resolve<T>();
        }

        public IEnumerable<T> ResolveAll<T>() where T : class
        {
            return _container.ResolveAll<T>();
        }


        public void Register<RegisterType, RegisterImpl>() where RegisterType : class where RegisterImpl : class, RegisterType
        {
            _container.Register<RegisterType, RegisterImpl>();
        }

        public void Register<RegisterType>(RegisterType instance) where RegisterType : class
        {
            _container.Register<RegisterType>(instance);
        }

        public IDependencyResolver CreateChild()
        {
            return new DefaultDependencyResolver(this);
        }

        public IDependencyResolver CreateChild(string name)
        {
            throw new NotSupportedException();
        }

        public IDependencyResolver CreateChild(Action<IDependencyBuilder> configurationAction)
        {
            return new DefaultDependencyResolver(this, configurationAction);

        }

        public IDependencyResolver CreateChild(string name, Action<IDependencyBuilder> configurationAction)
        {
            throw new NotSupportedException();
        }


        private void UpdateContainerFromBuilder(TinyIoCContainer _container, DefaultDependencyBuilder b)
        {
            var groupedRegistrations = b.Registrations.SelectMany(r => r.RegistrationTypes.Select(contract => new { registration = r, contract = contract })).GroupBy(r => r.contract);
            foreach (var r in groupedRegistrations)
            {
                if (r.Count() > 1)
                {
                    _container.RegisterMultiple(r.Key, r.Select(a => a.registration.SelfType));
                }
                else
                {
                    var registration = r.First();
                    var registrationType = registration.GetType().GetGenericTypeDefinition();
                }
            }
        }


        private static void TranslateInstanceRegistration<T>(TinyIoCContainer container, DefaultInstanceRegistrationBuilder<T> instanceRegistration) where T : class
        {
            var r = container.Register(typeof(T), instanceRegistration.Instance);
            ApplyOptions(r, instanceRegistration);

        }


        private static void TranslateConstructorRegistration<T>(TinyIoCContainer container, DefaultConstructorRegistrationBuilder<T> constructorRegistration) where T : class
        {
            var r = container.Register(typeof(T), constructorRegistration.SelfType);

            ApplyOptions(r, constructorRegistration);
        }

        private static void TranslateFactoryRegistration<T>(TinyIoCContainer container, DefaultFactoryRegistrationBuilder<T> factoryRegistration) where T : class
        {
            var r = container.Register<T>((c,p) => factoryRegistration.Factory(new DependencyResolverWrapper(c)));

            ApplyOptions(r,factoryRegistration);
        }

        private static void ApplyOptions(TinyIoCContainer.RegisterOptions r, DefaultRegistrationBuilderBase instanceRegistration)
        {
            object scopeConfigObj;
            var singleton = false;
            if (instanceRegistration.Options.TryGetValue("Singleton", out scopeConfigObj))
            {
                singleton = (bool)scopeConfigObj;
            }
            if (singleton)
            {
                r.AsSingleton();
            }
        }


        public void Dispose()
        {
            if (_container != null)
            {
                _container.Dispose();
            }
        }


        private class DependencyResolverWrapper : IDependencyResolver
        {
            private readonly TinyIoCContainer _container;
            public DependencyResolverWrapper(TinyIoCContainer container)
            {
                _container = container;
            }

            public IDependencyResolver CreateChild()
            {
                throw new NotImplementedException();
            }

            public IDependencyResolver CreateChild(Action<IDependencyBuilder> configurationAction)
            {
                throw new NotImplementedException();
            }

            public IDependencyResolver CreateChild(string name)
            {
                throw new NotImplementedException();
            }

            public IDependencyResolver CreateChild(string name, Action<IDependencyBuilder> configurationAction)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                
            }

            public T Resolve<T>() where T : class
            {
                return _container.Resolve<T>();
            }

            public IEnumerable<T> ResolveAll<T>() where T : class
            {
                return _container.ResolveAll<T>();
            }
        }
    }

    
}
