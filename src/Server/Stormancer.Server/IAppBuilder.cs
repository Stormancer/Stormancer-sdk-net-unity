using Stormancer.Core;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Object used to build a Stormancer server application.
    /// </summary>
    public interface IAppBuilder
    {
        /// <summary>
        /// Adds a plugin to the server application.
        /// </summary>
        /// <remarks>
        /// Some plugin are added automatically by the runtime:
        /// - RpcHostPlugin enable remote procedure calls between the client and server on both sides and partial responses to requests through the observable pattern.
        /// </remarks>
        /// <param name="plugin">The plugin to add</param>
        void AddPlugin(IHostPlugin plugin);
        /// <summary>
        /// Adds a scene template in the application.
        /// </summary>
        /// <param name="name">The name of the scene template.</param>
        /// <param name="factory">The factory method called to build a scene having this template.</param>
        /// <param name="metadata">Metadata associated with the template.</param>
        /// <returns>The current 'IAppBuilder' instance.</returns>
        IAppBuilder SceneTemplate(string name, Action<ISceneHost> factory, Dictionary<string,string> metadata = null);

    }
}
