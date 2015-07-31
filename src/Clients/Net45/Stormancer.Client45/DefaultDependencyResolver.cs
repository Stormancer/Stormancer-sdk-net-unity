using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        internal DefaultDependencyResolver(IDependencyResolver container)
        {
            if (container != null)
            {
                _container = ((DefaultDependencyResolver)container).Container;
            }
            else
            {
                _container = new TinyIoCContainer();
            }
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


    }
}
