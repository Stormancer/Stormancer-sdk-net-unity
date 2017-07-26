using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Networking
{
    public class PendingConnection
    {
        public PendingConnection (string endpoint, TaskCompletionSource<IConnection> tcs)
        {
            Endpoint = endpoint;
            Tcs = tcs;
        }

        public string Endpoint { get; private set; }
        public TaskCompletionSource<IConnection> Tcs { get; private set; }
    }
}
