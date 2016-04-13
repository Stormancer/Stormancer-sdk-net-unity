using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core.DependencyResolver
{
    public class DefaultDependencyBuilder : IDependencyBuilder
    {
        private List<DefaultRegistrationBuilderBase> _registrations = new List<DefaultRegistrationBuilderBase>();
        public IEnumerable<DefaultRegistrationBuilderBase> Registrations => _registrations.AsEnumerable();
        
        public IRegistrationBuilder Register<RegisterType>() where RegisterType : class
        {
            var result = new DefaultConstructorRegistrationBuilder<RegisterType>();
            _registrations.Add(result);

            return result;
        }

        public IRegistrationBuilder Register<RegisterType>(Func<IDependencyResolver, RegisterType> factory) where RegisterType : class
        {

            var result = new DefaultFactoryRegistrationBuilder<RegisterType>(factory);
            _registrations.Add(result);

            return result;
        }

        public IRegistrationBuilder Register<RegisterType>(RegisterType instance) where RegisterType:class
        {
            var result = new DefaultInstanceRegistrationBuilder<RegisterType>(instance);
            _registrations.Add(result);

            return result;
        }
    }
}
