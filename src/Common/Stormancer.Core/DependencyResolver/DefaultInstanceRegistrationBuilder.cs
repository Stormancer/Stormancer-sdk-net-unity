using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core.DependencyResolver
{

    public class DefaultInstanceRegistrationBuilder<T> : DefaultRegistrationBuilderBase where T : class
    {
        public T Instance { get; }

        public override Type SelfType => typeof(T);

        public DefaultInstanceRegistrationBuilder(T instance)
        {
            Instance = instance;
        }
    }
}
