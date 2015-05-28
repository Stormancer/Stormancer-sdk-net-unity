using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.Components
{
    /// <summary>
    /// Contains methods to get in
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        /// Returns informations about the current application.
        /// </summary>
        /// <returns>A Task returning an ApplicationInfos instance on completion.</returns>
        Task<ApplicationInfos> GetApplicationInfos();

        /// <summary>
        /// A boolean value indicating if the running environment is currently the active deployment.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Event fired when the active deployment change.
        /// </summary>
        event EventHandler<ActiveDeploymentChangedEventArgs> ActiveDeploymentChanged;

        /// <summary>
        /// Returns a dynamic object containing the configuration of the application.
        /// </summary>
        /// <returns></returns>
        dynamic Configuration { get; }


        /// <summary>
        /// Event fired when the application's configuration is updated.
        /// </summary>
        event EventHandler<EventArgs> ConfigurationChanged;


    }

    /// <summary>
    /// Contains informations about the new active deployment when the ActiveDeploymentChanged event is fired.
    /// </summary>
    public class ActiveDeploymentChangedEventArgs:EventArgs
    {
        /// <summary>
        /// A boolean value indicating whether the current deployment is now active.
        /// </summary>
        public bool IsActive{get;set;}

        /// <summary>
        /// A string containing the id of the active deployment.
        /// </summary>
        public string ActiveDeploymentId{get;set;}

     
    }
}
