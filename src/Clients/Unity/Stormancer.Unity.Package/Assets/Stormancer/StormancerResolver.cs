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
        T GetComponent<T>();
        Func<IDependencyResolver, T> GetComponentFactory<T>();
        void RegisterComponent<T>(Func<T> component);
        void RegisterComponent<T>(Func<IDependencyResolver, T> component);
    }

    public class StormancerResolver : IDependencyResolver
    {
        private readonly Dictionary<Type, Func<IDependencyResolver, object>> _registrations = new Dictionary<Type, Func<IDependencyResolver, object>>();
        private readonly StormancerResolver _parent = null;


        public StormancerResolver(StormancerResolver parent = null)
        {
            _parent = parent;
        }


        public T GetComponent<T>()
        {
            return GetComponentFactory<T>()(this);
        }

        public Func<IDependencyResolver, T> GetComponentFactory<T>()
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
                throw new InvalidOperationException(string.Format("The requested component of type {0} was not registered.", typeof(T)));
            }
        }

        public void RegisterComponent<T>(Func<T> component)
        {
            RegisterComponent(c => component());
        }

        public void RegisterComponent<T>(Func<IDependencyResolver, T> factory)
        {
            _registrations[typeof(T)] = c => factory(c);
        }

        public void RegisterComponent<T>(T component)
        {
            RegisterComponent(c => component);
        }

    }
}