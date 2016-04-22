using UnityEngine;
using Stormancer;
using Stormancer.Core;
using System;
using System.Collections;

namespace Stormancer.Plugins
{
    public class IConnectionHandler
    {
        /// <summary>
        /// Event fired when a connection is created
        /// </summary>
        public Action<PeerConnectedContext> PeerConnected { get; set; }

        ///// <summary>
        ///// Event fied when a connection is dismissed.
        ///// </summary>
        //public Action<> PeerDisconnected { get; set; }
    }
}