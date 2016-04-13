using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core.DependencyResolver
{
    public class DefaultConstructorRegistrationBuilder<T> : DefaultRegistrationBuilderBase where T : class
    {
        public override Type SelfType => typeof(T);
    }
}
