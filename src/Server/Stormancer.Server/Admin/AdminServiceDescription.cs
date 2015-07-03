using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server
{
    /// <summary>
    /// Describes a custom admin service available from the Stormancer api.
    /// </summary>
    public class AdminServicesDescription
    {

        /// <summary>
        /// Creates a new admin service description
        /// </summary>
        public AdminServicesDescription(string prefix)
        {
            Get = new Dictionary<string, Func<dynamic, dynamic>>();
            Delete = new Dictionary<string, Func<dynamic, dynamic>>();
            Put = new Dictionary<string, Func<dynamic, dynamic>>(); ;
            Post = new Dictionary<string, Func<dynamic, dynamic>>();
        }
        /// <summary>
        /// Url prefix
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Get services
        /// </summary>
        public IDictionary<string, Func<dynamic, dynamic>> Get
        {
            get;
            private set;
        }

        /// <summary>
        /// Delete service
        /// </summary>
        public IDictionary<string, Func<dynamic, dynamic>> Delete
        {
            get;
            private set;
        }

        /// <summary>
        /// Post service
        /// </summary>
        public IDictionary<string, Func<dynamic, dynamic>> Post
        {
            get;
            private set;
        }

        /// <summary>
        /// Put service
        /// </summary>
        public IDictionary<string, Func<dynamic, dynamic>> Put
        {
            get;
            private set;
        }
    }

}
