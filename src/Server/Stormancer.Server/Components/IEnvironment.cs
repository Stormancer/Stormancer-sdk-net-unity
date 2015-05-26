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
        /// Event fired when the active deployment change.
        /// </summary>
        event EventHandler<ActiveDeploymentChangedEventArgs> ActiveDeploymentChanged;

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
