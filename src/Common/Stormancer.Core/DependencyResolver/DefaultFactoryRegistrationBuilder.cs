using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core.DependencyResolver
{
    public class DefaultFactoryRegistrationBuilder<T> : DefaultRegistrationBuilderBase where T : class
    {
        public override Type SelfType => typeof(T);

        public Func<IDependencyResolver, T> Factory { get; }

        public DefaultFactoryRegistrationBuilder(Func<IDependencyResolver, T> factory)
        {
            Factory = factory;
        }
    }
}
