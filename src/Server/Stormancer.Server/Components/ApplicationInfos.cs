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
        /// Id of the active deployment for the application
        /// </summary>
        public string ActiveDeployment { get; set; }

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

        /// <summary>
        /// The url of the Api endpoint associated with the current environment.
        /// </summary>
        public string ApiEndpoint { get; set; }
    }
}
