using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    /// <summary>
    /// Represents a route on a scene.
    /// </summary>
    public class Route
    {
       
        public Route(IScene scene,string routeName, ushort index, Dictionary<string, string> metadata)
        {
            Name = routeName;
            Scene = scene;
            Index = index;
            if(metadata == null)
            {
                metadata = new Dictionary<string, string>();
            }
            Metadata = metadata;
        }

        /// <summary>
        /// The <see cref="Stormancer.Scene"/> instance that declares this route.
        /// </summary>
        public IScene Scene { get; private set; }

        /// <summary>
        /// A string containing the name of the route.
        /// </summary>
        public string Name { get; private set; }
        public ushort Index { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }
    }
}