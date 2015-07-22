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
        /// Lists the available storage indices available for the application.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Index>> ListIndices();

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

        /// <summary>
        /// Last known ping value between the host and the cluster node.
        /// </summary>
        /// <remarks>
        /// Ping values are updated approximately every 3 seconds.
        /// Does not represent the ping with the peers, but the internal ping between the node and the sandbox. An high ping value is an indicator of contention in the hosting system.
        /// </remarks>
        long LastPing
        {
            get;
        }

        /// <summary>
        /// Value of the synchronized clock (in ms)
        /// </summary>
        /// <remarks>
        /// The clock value  represents the number milliseconds since an arbitrary date. This value is synchronized between all peers (including server &amp; client peers).
        /// It's not designed to persist dates (for that you have DateTime) as it may drift as much as 1 ms every 20s.
        /// Furthermore, the value is reset on server restart. Don't persist it.
        /// </remarks>
        long Clock
        {
            get;
        }
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
