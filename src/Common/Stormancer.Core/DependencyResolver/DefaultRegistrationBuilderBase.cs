using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core.DependencyResolver
{ 
    public abstract class DefaultRegistrationBuilderBase : IRegistrationBuilder
    {
        public abstract Type SelfType { get; }

        private List<Type> _registrationTypes = new List<Type>();
        public IEnumerable<Type> RegistrationTypes => _registrationTypes.AsEnumerable();

        public bool PreserveExistingDefaults { get; private set; } = false;

        public Dictionary<string, object> Options { get; } = new Dictionary<string, object>();

        public IRegistrationBuilder As<RegisterType>()
        {
            return this.As(typeof(RegisterType));
        }

        public IRegistrationBuilder AsSelf()
        {
            return this.As(SelfType);
        }

        private IRegistrationBuilder As(Type registerType)
        {
            if (!_registrationTypes.Contains(registerType))
            {
                _registrationTypes.Add(registerType);
            }

            return this;
        }

        IRegistrationBuilder IRegistrationBuilder.PreserveExistingDefaults()
        {
            PreserveExistingDefaults = true;
            return this;
        }

        public IRegistrationBuilder AddOptions(Action<Dictionary<string, object>> handler)
        {
            handler(Options);
            return this;
        }
    }
}
