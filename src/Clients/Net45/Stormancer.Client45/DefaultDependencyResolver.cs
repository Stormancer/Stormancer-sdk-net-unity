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

        internal DefaultDependencyResolver() : this(null, b => { })
        {

        }
        internal DefaultDependencyResolver(Action<IDependencyBuilder> builder) : this(null, builder)
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
            throw new NotSupportedException("Named scope not supported");
        }

        public IDependencyResolver CreateChild(Action<IDependencyBuilder> configurationAction)
        {
            return new DefaultDependencyResolver(this, configurationAction);

        }

        public IDependencyResolver CreateChild(string name, Action<IDependencyBuilder> configurationAction)
        {
            throw new NotSupportedException("Named scope not supported");
        }


        private void UpdateContainerFromBuilder(TinyIoCContainer _container, DefaultDependencyBuilder b)
        {
            var groupedRegistrations = b.Registrations.SelectMany(r => r.RegistrationTypes.Select(contract => new { registration = r, contract = contract })).GroupBy(r => r.contract).ToArray() ;
            foreach (var r in groupedRegistrations)
            {
                if (r.Count() > 1)
                {
                    _container.RegisterMultiple(r.Key, r.Select(a => a.registration.SelfType));
                }
                else
                {
                    var registration = r.First();
                    var registrationType = registration.registration.GetType().GetGenericTypeDefinition();
                    var t = registration.registration.GetType().GetGenericArguments()[0];

                    if (TranslateInstanceRegistration(_container, registration.registration, t))
                    {
                        
                    }
                    else if (TranslateConstructorRegistration(_container, registration.registration, t))
                    {
                      
                    }
                    else if (TranslateFactoryRegistration(_container, registration.registration, t))
                    {
                        
                    }
                }
            }
        }


        private static bool TranslateInstanceRegistration(TinyIoCContainer container, DefaultRegistrationBuilderBase instanceRegistration, Type t)
        {
            var registrationType = instanceRegistration.GetType().GetGenericTypeDefinition();
            if (registrationType != typeof(DefaultInstanceRegistrationBuilder<>))
            {
                return false;
            }
            var instance = instanceRegistration.GetType().GetProperty("Instance").GetGetMethod().Invoke(instanceRegistration, null);
            var r = container.Register(t, instance);
            ApplyOptions(r, instanceRegistration);

            return true;
        }


        private static bool TranslateConstructorRegistration(TinyIoCContainer container, DefaultRegistrationBuilderBase constructorRegistration, Type t)
        {
            var registrationType = constructorRegistration.GetType().GetGenericTypeDefinition();
            if (registrationType != typeof(DefaultConstructorRegistrationBuilder<>))
            {
                return false;
            }

            var r = container.Register(t, constructorRegistration.SelfType);

            ApplyOptions(r, constructorRegistration);
            return true;
        }

        private static bool TranslateFactoryRegistration(TinyIoCContainer container, DefaultRegistrationBuilderBase factoryRegistration, Type t)
        {
            var registrationType = factoryRegistration.GetType().GetGenericTypeDefinition();
            if (registrationType != typeof(DefaultFactoryRegistrationBuilder<>))
            {
                return false;
            }
            var factory = (Delegate)factoryRegistration.GetType().GetProperty("Factory").GetGetMethod().Invoke(factoryRegistration, null);
            var r = container.Register(t, (c, p) => factory.DynamicInvoke(new DependencyResolverWrapper(c)));

            ApplyOptions(r, factoryRegistration);

            return true;
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

    /// <summary>
    /// Extension methods for Registration builder
    /// </summary>
    public static class RegistrationBuilderExtensions
    {
        /// <summary>
        /// Declares the registration as a singleton.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static IRegistrationBuilder Singleton(this IRegistrationBuilder b)
        {
            b.AddOptions(o => o.Add("Singleton", true));
            return b;
        }
    }


}
