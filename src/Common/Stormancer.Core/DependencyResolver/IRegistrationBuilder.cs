using System;
using System.Collections.Generic;

namespace Stormancer
{
    public interface IRegistrationBuilder
    {
        IRegistrationBuilder AddOptions(Action<Dictionary<string, object>> handler);

        IRegistrationBuilder As<RegisterType>();
        IRegistrationBuilder AsSelf();

        IRegistrationBuilder PreserveExistingDefaults();
    } 
}