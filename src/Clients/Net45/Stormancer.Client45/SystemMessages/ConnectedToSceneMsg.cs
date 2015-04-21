using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
  
    /// <summary>
    /// Dto containing the parameters required to connect to a scene
    /// </summary>
    public struct ConnectToSceneMsg
    {
        /// <summary>
        /// Authentication token
        /// </summary>
        public string Token;

        /// <summary>
        /// List of client routes
        /// </summary>
        public List<RouteDto> Routes;

        /// <summary>
        /// Client scene object metadata
        /// </summary>
        public Dictionary<string, string> SceneMetadata { get; set; }

        /// <summary>
        /// Connection metadata
        /// </summary>
        public Dictionary<string, string> ConnectionMetadata { get; set; }
    }
}
