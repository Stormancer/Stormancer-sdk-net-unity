using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Contract for the binary serializers used by Stormancer applications.
    /// </summary>
    /// <remarks>
    /// Implement this interface if you want to replace the default Stormancer serializer with your own.
    /// 
    /// In this case, you *must* use the same serializer on your clients and server application.
    /// </remarks>
    public interface ISerializer
    {
        /// <summary>
        /// Serialize an object into a stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="data">The object to serialize.</param>
        /// <param name="stream">The output stream.</param>
        void Serialize<T>(T data, Stream stream);

        /// <summary>
        /// Deserialize an object from a stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="stream">The input stream.</param>
        /// <returns>A T instance deserialized from the stream.</returns>
        T Deserialize<T>(Stream stream);

        /// <summary>
        /// The serializer format.
        /// </summary>
        string Name { get; }
    }
}
