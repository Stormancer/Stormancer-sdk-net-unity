using Stormancer.Plugins;
using System;
using System.Collections.Generic;
namespace Stormancer.Server.Admin
{
    /// <summary>
    /// Describes an admin plugin
    /// </summary>
    public interface IAdminPluginConfig
    {
       

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
        /// 'Get' routes for the module
        /// </summary>
        IDictionary<string, Func<dynamic, dynamic>> Get { get; }

        /// <summary>
        /// 'Delete' routes for the module
        /// </summary>
        IDictionary<string, Func<dynamic, dynamic>> Delete { get; set; }

        /// <summary>
        /// 'Post' routes for the module
        /// </summary>
        IDictionary<string, Func<dynamic, dynamic>> Post { get; set; }

        /// <summary>
        /// 'Put' routes for the module
        /// </summary>
        IDictionary<string, Func<dynamic, dynamic>> Put { get; set; }

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
