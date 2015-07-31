using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Dependency resolver interface for Stormancer.
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Resolves a single object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Resolves all the registrations for the given contract.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> ResolveAll<T>() where T : class;


        /// <summary>
        /// Registers a type in the container
        /// </summary>
        /// <typeparam name="RegisterType"></typeparam>
        /// <typeparam name="RegisterImpl"></typeparam>
        void Register<RegisterType, RegisterImpl>() where RegisterType : class where RegisterImpl : class, RegisterType;

        /// <summary>
        /// Register an instance in the container
        /// </summary>
        /// <typeparam name="RegisterType"></typeparam>
        /// <param name="instance"></param>
        void Register<RegisterType>(RegisterType instance) where RegisterType : class;
    }
}
