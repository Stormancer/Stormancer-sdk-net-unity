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
        /// Id of the plugin (directory)
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Sets the name of the admin plugin tab
        /// </summary>
        /// <param name="name">Name of the plugin tab</param>
        /// <returns>Current instance of the plugin config object</returns>
        IAdminPluginConfig Name(string name);


        /// <summary>
        /// Configure Admin Web API
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        IAdminPluginConfig ConfigureApi(Action<Owin.IAppBuilder> builder);
    }

   
}
