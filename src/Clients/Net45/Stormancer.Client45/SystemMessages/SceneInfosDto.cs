using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    /// <summary>
    /// Dto containing the parameters required to perform a get scene infos request
    /// </summary>
    public struct SceneInfosRequestDto
    {
        /// <summary>
        /// Authentication token containing informations about the target scene
        /// </summary>
        public string Token;
        /// <summary>
        /// Connection metadata
        /// </summary>
        public Dictionary<string, string> Metadata;
    }

    /// <summary>
    /// Dto containing the result from a get scene infos request
    /// </summary>
    public struct SceneInfosDto
    {
        /// <summary>
        /// Scene id
        /// </summary>
        public string SceneId;

        /// <summary>
        /// Scene metadata
        /// </summary>
        public Dictionary<string, string> Metadata;

        /// <summary>
        /// List of routes declared on the scene host
        /// </summary>
        public List<RouteDto> Routes;

        /// <summary>
        /// The serializer the client should use when communicating with the scene
        /// </summary>
        public string SelectedSerializer;
    }
}
