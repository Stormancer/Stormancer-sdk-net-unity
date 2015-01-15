﻿using RakNet;
using Stormancer.Core;
using Stormancer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Networking
{
   
    internal class RakNetConnection : IConnection
    {
        private RakPeerInterface _rakPeer;
        private RakNetGUID _guid;

        private readonly Action<RakNetConnection> _closeAction;
        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();
        internal RakNetConnection(RakNetGUID guid, long id, RakPeerInterface peer,
            Action<RakNetConnection> closeAction)
        {
            ConnectionDate = DateTime.UtcNow;
            LastActivityDate = DateTime.UtcNow;
            Id = id;
            _guid = guid;

            _rakPeer = peer;
            _closeAction = closeAction;
            State = Stormancer.Core.ConnectionState.Connecting;
        }

        /// <summary>
        /// Id of the connection
        /// </summary>
        public RakNetGUID Guid
        {
            get
            {
                return _guid;
            }
        }

        /// <summary>
        /// Connection date
        /// </summary>
        public DateTime ConnectionDate { get; internal set; }

        /// <summary>
        /// Last activity 
        /// </summary>
        public DateTime LastActivityDate { get; internal set; }


        /// <summary>
        /// IP address of the connection.
        /// </summary>
        public string IpAddress
        {
            get
            {
                return _rakPeer.GetSystemAddressFromGuid(_guid).ToString();
            }
        }

        /// <summary>
        /// The account id of the application to which this connection is connected.
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// The id of the application to which this connection is connected.
        /// </summary>
        public string Application { get; set; }

        public Stormancer.Core.ConnectionState State { get; private set; }


        public long Id
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns an hashcode based on Id.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Equals implementation for IConnection
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var v2 = obj as IConnection;
            if(v2 == null)
            {
                return false;
            }
            else
            {
                return v2.Id == this.Id;
            }
        }

        /// <summary>
        /// Connection metadata
        /// </summary>
        public Dictionary<string, string> Metadata
        {
            get { return _metadata; }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            _closeAction(this);
        }

        /// <summary>
        /// Sends a system request to the remote peer.
        /// </summary>
        /// <param name="msgId">Id of the system message</param>
        /// <param name="writer"></param>
        public void SendSystem(byte msgId, Action<System.IO.Stream> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            var stream = new BitStream();
            stream.Write(msgId);
            writer(new BSStream(stream));
            var result = _rakPeer.Send(stream, RakNet.PacketPriority.HIGH_PRIORITY, RakNet.PacketReliability.RELIABLE, (char)0, this.Guid, false);
            if (result == 0)
            {
                throw new InvalidOperationException("Failed to send message.");
            }
        }

        /// <summary>
        /// Sends a scene request to the remote peer.
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="route"></param>
        /// <param name="writer"></param>
        /// <param name="priority"></param>
        /// <param name="reliability"></param>
        /// <param name="channel"></param>
        public void SendToScene(byte sceneIndex, ushort route, Action<System.IO.Stream> writer, Stormancer.Core.PacketPriority priority, Stormancer.Core.PacketReliability reliability, char channel)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            var stream = new BitStream();
            stream.Write(sceneIndex);
            stream.Write(route);
            writer(new BSStream(stream));
            var result = _rakPeer.Send(stream, (RakNet.PacketPriority)priority, (RakNet.PacketReliability)reliability, channel, this.Guid, false);

            if (result == 0)
            {
                throw new InvalidOperationException("Failed to send message.");
            }
        }


        public Action<string> ConnectionClosed
        {
            get;
            set;
        }

        

        private Dictionary<string, object> _localData = new Dictionary<string, object>();
        public Dictionary<string, object> Components
        {
            get { return _localData; }
        }

        public T GetComponent<T>(string key)
        {
            return (T)_localData[key];
        }
    }
}
