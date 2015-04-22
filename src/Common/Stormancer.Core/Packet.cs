using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// A packet sent by a remote peer to the running peer.
    /// </summary>
    public class Packet<T>
    {
        public Packet(T source, Stream stream) : this(source,stream,new Dictionary<string, object>())
        {
          
        }

        public Packet(T source, Stream stream, Dictionary<string, object> metadata)
        {
              Connection = source;

            Stream = stream;
            Metadata = metadata;
        }

        /// <summary>
        /// Data contained in the packet.
        /// </summary>
        public Stream Stream
        {
            get;
            private set;
        }

        /// <summary>
        /// Metadata stored by the packet.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Reads and return metadata casted to the requested type.
        /// </summary>
        /// <typeparam name="T">The returned metadata type.</typeparam>
        /// <param name="key">A string containing a metadata key.</param>
        /// <returns>The metadata for the *key* as a `T`</returns>
        public TData GetMetadata<TData>(string key)
        {
            return (TData)this.Metadata[key];
        }

        /// <summary>
        /// The remote peer that sent the packet.
        /// </summary>
        public T Connection
        {
            get;
            private set;
        }
    }

    public class Packet : Packet<IConnection>
    {
        public Packet(IConnection source, Stream stream) : base(source, stream) { }
    }
}
