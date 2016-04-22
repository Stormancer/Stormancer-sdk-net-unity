using System;

namespace Stormancer
{
    public interface IDependencyBuilder
    {
        IRegistrationBuilder Register<RegisterType>() where RegisterType : class;

        IRegistrationBuilder Register<RegisterType>(RegisterType instance) where RegisterType : class;

        IRegistrationBuilder Register<RegisterType>(Func<IDependencyResolver, RegisterType> factory) where RegisterType : class;
    }
}