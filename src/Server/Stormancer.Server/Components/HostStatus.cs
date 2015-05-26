using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Server.Components
{
    /// <summary>
    /// Informations about the running application
    /// </summary>
    public class ApplicationInfos
    {
        /// <summary>
        /// A boolean value indicating whether the current host is the active deployment.
        /// </summary>
        public bool IsActiveDeployment { get; set; }

        /// <summary>
        /// A string containing the id of the current deployment.
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// A string containing the name of the current application.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// A string containing the id of the account containing the application.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// A string containing the primary access key of the application.
        /// </summary>
        public string PrimaryKey { get; set; }

        /// <summary>
        /// A string containing the secondary access key of the application.
        /// </summary>
        public string SecondaryKey { get; set; }
    }
}
