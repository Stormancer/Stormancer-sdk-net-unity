
using Stormancer.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Stormancer
{
    [RequireComponent(typeof(StormancerNetworkIdentity))]
    public class EventSender : MonoBehaviour
    {
        private StormancerNetworkIdentity _identity;

        private readonly ConcurrentDictionary<byte, Action<long, Stream>> _untargettedCallbacks = new ConcurrentDictionary<byte, Action<long, Stream>>();
        private readonly ConcurrentDictionary<byte, Action<long, StormancerNetworkIdentity, Stream>> _targettedCallbacks = new ConcurrentDictionary<byte, Action<long, StormancerNetworkIdentity, Stream>>();

        public void Awake()
        {
            _identity = GetComponent<StormancerNetworkIdentity>();
        }

        public void SendEvent(byte eventId, PacketReliability reliability, Action<Stream> writer)
        {
            var replicator = _identity.Replicator;
            if (replicator != null)
            {
                replicator.SendEvent(_identity, eventId, reliability, writer);
            }
        }

        public void SendEvent(StormancerNetworkIdentity target, byte eventId, PacketReliability reliability, Action<Stream> writer)
        {
            var replicator = _identity.Replicator;
            if (replicator != null)
            {
                replicator.SendEvent(_identity, target, eventId, reliability, writer);
            }
        }

        public void ReceiveEvent(byte eventId, long timeStamp, Stream stream)
        {
            Action<long, Stream> registration;
            if (_untargettedCallbacks.TryGetValue(eventId, out registration) && registration != null)
            {
                registration(timeStamp, stream);
            }
        }

        internal void ReceiveEvent(byte eventId, StormancerNetworkIdentity target, long timeStamp, Stream stream)
        {
            Action<long, StormancerNetworkIdentity, Stream> registration;
            if (_targettedCallbacks.TryGetValue(eventId, out registration) && registration != null)
            {
                registration(timeStamp, target, stream);
            }
        }

        public IDisposable SubscribeToEvent(byte eventId, Action<long, Stream> callback)
        {
            Action<long, Stream> previousRegistration;

            if (_untargettedCallbacks.TryGetValue(eventId, out previousRegistration))
            {
                previousRegistration += callback;
            }
            else
            {
                _untargettedCallbacks.TryAdd(eventId, callback);
            }

            return new DisposableAction(() =>
            {
                Action<long, Stream> registration;
                if (_untargettedCallbacks.TryGetValue(eventId, out registration))
                {
                    registration -= callback;
                }
            });
        }

        public IDisposable SubscribeToEvent(byte eventId, Action<long, StormancerNetworkIdentity, Stream> callback)
        {
            Action<long, StormancerNetworkIdentity, Stream> previousRegistration;

            if (_targettedCallbacks.TryGetValue(eventId, out previousRegistration))
            {
                previousRegistration += callback;
            }
            else
            {
                _targettedCallbacks.TryAdd(eventId, callback);
            }

            return new DisposableAction(() =>
            {
                Action<long, StormancerNetworkIdentity, Stream> registration;
                if (_targettedCallbacks.TryGetValue(eventId, out registration))
                {
                    registration -= callback;
                }
            });
        }
    }
}