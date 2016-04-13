using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer.Server.Admin;
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
        /// Configures the dependency resolver for the application.
        /// </summary>
        /// <param name="configurationAction">An action configuring the dependency resolver to add new dependencies to the child resolver.</param>
        /// <remarks>
        /// Multiple calls will result in all the configuration actions being called sequentially.
        /// </remarks>
        void ConfigureDependencies(Action<IDependencyBuilder> configurationAction);

        /// <summary>
        /// Adds a scene template in the application.
        /// </summary>
        /// <param name="name">The name of the scene template.</param>
        /// <param name="factory">The factory method called to build a scene having this template.</param>
        /// <param name="metadata">Metadata associated with the template.</param>
        /// <returns>The current 'IAppBuilder' instance.</returns>
        IAppBuilder SceneTemplate(string name, Action<ISceneHost> factory, Dictionary<string,string> metadata = null);

        /// <summary>
        /// Adds a new admin plugin
        /// </summary>
        /// <param name="id">id of the plugin</param>
        /// <param name="version">target admin plugin host version</param>
        /// <returns></returns>
        IAdminPluginConfig AdminPlugin(string id, AdminPluginHostVersion version);

        /// <summary>
        /// Configure the local web server using the provided configuration method.
        /// </summary>
        /// <remarks>
        /// The server can't be enabled currently. <br/>The method can be called several times. In this case, the configuration methods will be applied in the order they were added.
        /// </remarks>
        /// <param name="config">Configuration method</param>
        void WebServer(Action<Owin.IAppBuilder> config);
    }
}
