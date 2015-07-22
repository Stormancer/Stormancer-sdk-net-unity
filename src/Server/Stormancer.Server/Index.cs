using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server
{
    /// <summary>
    /// Represents an Elasticsearch index
    /// </summary>
    public class Index
    {
        /// <summary>
        /// Id of the account containing the index.
        /// </summary>
        public string accountId { get; set; }

        /// <summary>
        /// Name of the index.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Maximum size of the index in bytes.
        /// </summary>
        public int maxSize { get; set; }

        /// <summary>
        /// Current size of the index in bytes.
        /// </summary>
        public int size { get; set; }

        /// <summary>
        /// Primary access key.
        /// </summary>
        public string primaryKey { get; set; }

        /// <summary>
        /// Secondary access key.
        /// </summary>
        public string secondaryKey { get; set; }

        /// <summary>
        /// Server endpoint uri
        /// </summary>
        public string serverEndpoint { get; set; }
    }
}
