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
        public PendingConnection (IEnumerable<string> endpoints, TaskCompletionSource<IConnection> tcs)
        {
            Endpoints = endpoints;
            Tcs = tcs;
        }

        public IEnumerable<string> Endpoints { get; private set; }
        public TaskCompletionSource<IConnection> Tcs { get; private set; }
    }
}
