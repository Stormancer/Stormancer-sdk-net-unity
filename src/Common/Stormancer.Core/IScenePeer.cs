using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// A remote scene.
    /// </summary>
    public interface IScenePeer
    {
        /// <summary>
        /// Sends a message to the remote scene.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="writer"></param>
        /// <param name="priority"></param>
        /// <param name="reliability"></param>
        void Send(
            string route,
            Action<Stream> writer,
            PacketPriority priority,
            PacketReliability reliability);


        T GetComponent<T>();

        long Id { get; }

        string SceneId { get; }
        uint ShardId { get; }

    }

    
}
