using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Represents a route on a scene.
    /// </summary>
    public class Route
    {
        public Route(IScene scene, string routeName, ushort handle, Dictionary<string, string> metadata)
        {
            Name = routeName;
            Scene = scene;

            if (metadata == null)
            {
                metadata = new Dictionary<string, string>();
            }
            Metadata = metadata;
            Handle = handle;
        }

        public Route(IScene scene, string routeName, Dictionary<string, string> metadata)
            : this(scene, routeName, 0, metadata)
        {
        }

        /// <summary>
        /// The <see cref="Stormancer.Scene"/> instance that declares this route.
        /// </summary>
        public IScene Scene { get; private set; }

        /// <summary>
        /// A string containing the name of the route.
        /// </summary>
        public string Name { get; private set; }
        public ushort Handle { get; set; }
        public Dictionary<string, string> Metadata { get; private set; }

        public Action<Packet> Handlers { get; set; }
    }
}