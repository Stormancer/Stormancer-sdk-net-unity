using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    public interface IConnection
    {
        /// <summary>
        /// Unique id in the node for the connection.
        /// </summary>
        long Id { get; }

        /// <summary>
        /// Ip address of the remote peer.
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// Connection date.
        /// </summary>
        DateTime ConnectionDate { get; }

        /// <summary>
        /// Metadata associated with the connection.
        /// </summary>
        Dictionary<string, string> Metadata { get; }

        /// <summary>
        /// Data & services associated with the object.
        /// </summary>
        Dictionary<string, object> Components { get; }

        /// <summary>
        /// Gets a service from the object.
        /// </summary>
        /// <typeparam name="T">Type of the service to fetch.</typeparam>
        /// <param name="key">A string containing the service key.</param>
        /// <returns>A service object.</returns>
        T GetComponent<T>(string key);

        /// <summary>
        /// Account of the application which the peer is connected to.
        /// </summary>
        string Account { get; }

        /// <summary>
        /// Name of the application to which the peer is connected.
        /// </summary>
        string Application { get; }

        /// <summary>
        /// State of the connection.
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// Close the connection
        /// </summary>
        void Close();

        /// <summary>
        /// Sends a system message to the remote scene.
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="writer"></param>
        void SendSystem(byte msgId, Action<Stream> writer);

        /// <summary>
        /// Sends a packet to the target remote scene.
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="route"></param>
        /// <param name="writer"></param>
        /// <param name="priority"></param>
        /// <param name="reliability"></param>
        /// <param name="channel"></param>
        void SendToScene(byte sceneIndex,
            ushort route,
            Action<Stream> writer,
            PacketPriority priority,
            PacketReliability reliability,
            char channel);

     

        /// <summary>
        /// Event fired when the connection has been closed
        /// </summary>
        Action<string> ConnectionClosed { get; set; }

       

    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }
}
