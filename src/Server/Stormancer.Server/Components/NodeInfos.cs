using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.Components
{
    /// <summary>
    /// Informations about the node
    /// </summary>
    public class NodeInfos
    {
        /// <summary>
        /// Unique id of the node in the cluster
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the node in the cluster
        /// </summary>
        public string Name { get; set; }

       /// <summary>
       /// Public ip of the node, if available (empty otherwise)
       /// </summary>
        public string PublicIp { get; set; }
    }
}
