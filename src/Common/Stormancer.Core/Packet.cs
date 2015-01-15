using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    /// <summary>
    /// A packet sent by a remote peer to the running peer.
    /// </summary>
    public class Packet 
    {
       
        public Packet(IConnection source, Stream stream)
        {
            Connection = source;
          
            Stream = stream;
            Metadata = new Dictionary<string, object>();
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
        public T GetMetadata<T>(string key)
        {
            return (T)this.Metadata[key];
        }

        /// <summary>
        /// The remote peer that sent the packet.
        /// </summary>
        public IConnection Connection
        {
            get;
            private set;
        }
    }
}
