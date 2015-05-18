using Stormancer.Core;
using Stormancer.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Represents a peer connected to a scene on the server
    /// </summary>
    /// <remarks>
    /// IConnection &amp; IScenePeer are different beasts in Stormancer. IConnection represents a connection to the current client, 
    /// whereas IScenePeer objects are the link between IConnection objects and scene objects. The Stormancer runtime will create as many
    /// peer object as there are couples connection &lt;--&gt; scene.
    /// 
    /// </remarks>
    public interface IScenePeerClient : IScenePeer
    {
        /// <summary>
        /// Disconnects the peer from the scene.
        /// </summary>
        Task Disconnect(string reason);

        /// <summary>
        /// User data provided as part of the connection token.
        /// </summary>
        byte[] UserData { get; }

        /// <summary>
        /// Content type of the data in the connection token.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Metadata associated with the connection.
        /// </summary>
        Dictionary<string, string> Metadata { get; }

        /// <summary>
        /// Ip address of the remote peer
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// Routes declared on the client.
        /// </summary>
        IEnumerable<RouteDto> Routes { get; }

        /// <summary>
        /// The scene host this peer is connected to.
        /// </summary>
        ISceneHost Host { get; }
    }
}
