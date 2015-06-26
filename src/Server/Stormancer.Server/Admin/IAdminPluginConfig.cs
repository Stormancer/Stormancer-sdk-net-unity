using Stormancer.Plugins;
using System;
namespace Stormancer.Server.Admin
{
    /// <summary>
    /// Describes an admin plugin
    /// </summary>
    public interface IAdminPluginConfig
    {
        /// <summary>
        /// Additionnal content type mappings declared in the plugin
        /// </summary>
        System.Collections.Generic.IReadOnlyDictionary<string, string> ContentTypeMappings { get; }

        /// <summary>
        /// Adds a new content type mapping
        /// </summary>
        /// <param name="extension">File extension</param>
        /// <param name="mime">content type</param>
        /// <returns>Current instance of the plugin config object</returns>
        IAdminPluginConfig ContentTypeMapping(string extension, string mime);

        /// <summary>
        /// Gets the name of the admin plugin tab
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Sets the name of the admin plugin tab
        /// </summary>
        /// <param name="name">Name of the plugin tab</param>
        /// <returns>Current instance of the plugin config object</returns>
        IAdminPluginConfig Name(string name);

        /// <summary>
        /// Adds a service to the plugin
        /// </summary>
        /// <remarks>
        /// Services can be called from the admin plugin page and use the Stormancer RPC protocol.
        /// </remarks>
        /// <param name="id">id of the service</param>
        /// <param name="handler">Handler </param>
        /// <returns>Current instance of the plugin config object</returns>
        IAdminPluginConfig Service(string id, Func< RequestContext<IScenePeerClient>, System.Threading.Tasks.Task> handler);

        /// <summary>
        /// List of registered services
        /// </summary>
        System.Collections.Generic.IReadOnlyDictionary<string, Func< RequestContext<IScenePeerClient>, System.Threading.Tasks.Task>> Services { get; }
    }

    /// <summary>
    /// List of know admin plugin host versions
    /// </summary>
    public enum AdminPluginHostVersion
    {
        /// <summary>
        /// 0.1
        /// </summary>
        V0_1
    }
}
