using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    /// <summary>
    /// Represents a Stormancer scene.
    /// </summary>
    /// <remarks>
    /// In a Stormancer application, users connect to scenes to interact with each other and the application. 
    /// A scene has 2 faces: A scene host, currently only serverside scene hosts are supported, and scene clients.
    /// </remarks>
    public interface IScene
    {
        string Id { get; }

        /// <summary>
        /// Adds a route handler to the scene
        /// </summary>
        /// <param name="route">The name of the route</param>
        /// <param name="handler">The route handler</param>
        /// <param name="metadata">Optional metadata</param>
        void AddRoute(string route, Action<Packet> handler, Dictionary<string,string> metadata = null);

      

        /// <summary>
        /// True if the instance is an host. False if it's a client.
        /// </summary>
        bool IsHost { get; }
    }

   
}
