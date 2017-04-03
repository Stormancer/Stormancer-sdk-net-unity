using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins.Authentication
{
    public enum GameConnectionState
    {
        Disconnected,
        Connecting,
        Authenticated,
        Disconnecting,
        Authenticating
    }
}
