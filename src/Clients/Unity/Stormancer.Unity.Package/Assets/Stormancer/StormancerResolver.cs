using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Stormancer.Networking;
using Stormancer.Core;
using Stormancer.Plugins;
using System.Diagnostics;

namespace Stormancer
{
    public interface IDependencyResolver
    {
        T Resolve<T>();
        bool TryResolve<T>(out T dependency);
        //Func<IDependencyResolver, T> GetComponentFactory<T>();
        void Register<T>(Func<T> component);
        void Register<T>(Func<IDependencyResolver, T> component);
    }

    public class StormancerResolver : IDependencyResolver
    {
        private readonly Dictionary<Type, Func<IDependencyResolver, object>> _registrations = new Dictionary<Type, Func<IDependencyResolver, object>>();
        private readonly StormancerResolver _parent = null;


        public StormancerResolver(StormancerResolver parent = null)
        {
            _parent = parent;
        }


        public T Resolve<T>()
        {
            T result;
            if (!TryResolve(out result))
            {
                throw new InvalidOperationException(string.Format("The requested component of type {0} was not registered.", typeof(T)));
            }
            return result;
        }

        public bool TryResolve<T>(out T dependency)
        {
            var factory = GetComponentFactory<T>();
            if (factory != null)
            {
                dependency = factory(this);
                return true;
            }
            else
            {
                dependency = default(T);
                return false;
            }
        }

        private Func<IDependencyResolver, T> GetComponentFactory<T>()
        {
            Func<IDependencyResolver, object> factory;
            if (_registrations.TryGetValue(typeof(T), out factory))
            {
                return resolver => (T)(factory(resolver));
            }
            else if (_parent != null)
            {
                return _parent.GetComponentFactory<T>();
            }
            else
            {
                return null;
            }
        }

        public void Register<T>(Func<T> component)
        {
            Register(c => component());
        }

        public void Register<T>(Func<IDependencyResolver, T> factory)
        {
            _registrations[typeof(T)] = c => factory(c);
        }

        public void RegisterComponent<T>(T component)
        {
            Register(c => component);
        }
    }
}