using Stormancer.Proxy.Agent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Stormancer.Networking;
using Stormancer.Core;

namespace Stormancer.Networking.Processors
{
    public class RequestProcessor : IPacketProcessor
    {
        private class Request
        {
            public DateTime lastRefresh;
            public ushort id;
            public IObserver<Packet> observer;
            public TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        }
        private readonly ConcurrentDictionary<ushort, Request> _pendingRequests;
        private readonly ILogger _logger;

        private bool isRegistered = false;

        public RequestProcessor(ILogger logger, IEnumerable<IRequestModule> modules)
        {
            _pendingRequests = new ConcurrentDictionary<ushort, Request>();

            _logger = logger;

            var builder = new RequestModuleBuilder(this.AddSystemRequestHandler);
            foreach (var module in modules)
            {
                module.Register(builder);
            }
        }

        public void RegisterProcessor(PacketProcessorConfig config)
        {
            isRegistered = true;
            foreach (var handler in _handlers)//Add system request handlers
            {
                config.AddProcessor(handler.Key, p =>
                {

                    var context = new RequestContext(p);
                    handler.Value(context).ContinueWith(task =>
                    {
                        if (!context.IsComplete)
                        {
                            if (task.IsFaulted)
                            {
                                var clientException = task.Exception.InnerExceptions.OfType<ClientException>().FirstOrDefault();
                                var msg = clientException != null ? clientException.Message : "An error occured on the server.";
                                context.Error(s => p.Connection.GetComponent<ISerializer>("serializer").Serialize(msg, s));
                            }
                            else
                            {
                                context.Complete();
                            }
                        }

                    });
                    return true;
                });
            }
            config.AddProcessor((byte)MessageIDTypes.ID_REQUEST_RESPONSE_MSG, p =>
            {
                var temp = new byte[2];
                p.Stream.Read(temp, 0, 2);
                var id = BitConverter.ToUInt16(temp, 0);

                Request request;
                if (_pendingRequests.TryGetValue(id, out request))
                {
                    p.Metadata["request"] = request;
                    request.lastRefresh = DateTime.UtcNow;
                    request.observer.OnNext(p);
                    request.tcs.TrySetResult(true);
                }
                else
                {
                    _logger.Trace("Unknown request id.");
                }

                return true;
            });
            config.AddProcessor((byte)MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, p =>
            {
                var temp = new byte[2];
                p.Stream.Read(temp, 0, 2);
                var id = BitConverter.ToUInt16(temp, 0);
                var hasValues = p.Stream.ReadByte() == 1;

                Request request;
                if (_pendingRequests.TryGetValue(id, out request))
                {
                    p.Metadata["request"] = request;
                }
                else
                {
                    _logger.Trace("Unknown request id.");
                }

                this._pendingRequests.TryRemove(id, out request);
                if (hasValues)
                {
                    request.tcs.Task.ContinueWith(t => request.observer.OnCompleted());
                }
                else
                {
                    request.observer.OnCompleted();
                }
                return true;
            });
            config.AddProcessor((byte)MessageIDTypes.ID_REQUEST_RESPONSE_ERROR, p =>
            {
                var temp = new byte[2];
                p.Stream.Read(temp, 0, 2);
                var id = BitConverter.ToUInt16(temp, 0);
                Request request;
                if (_pendingRequests.TryGetValue(id, out request))
                {
                    p.Metadata["request"] = request;
                }
                else
                {
                    _logger.Trace("Unknown request id.");
                }

                this._pendingRequests.TryRemove(id, out request);
                var s = p.Connection.GetComponent<ISerializer>("serializer");
                var msg = s.Deserialize<string>(p.Stream);

                request.observer.OnError(new ClientException(msg));


                return true;
            });
        }

        public IObservable<Packet> SendSystemRequest(IConnection peer, byte msgId, Action<Stream> writer)
        {
            return Observable.Create<Packet>(observer =>
            {
                var request = ReserveRequestSlot(observer);
                peer.SendSystem(msgId, bs =>
                {
                    var bw = new BinaryWriter(bs);
                    bw.Write(request.id);
                    bw.Flush();
                    writer(bs);

                });
                return () =>
                {
                    Request r;
                    if (_pendingRequests.TryGetValue(request.id, out r))
                    {
                        if (r == request)
                        {
                            _pendingRequests.TryRemove(request.id, out request);
                        }
                    }

                };
            });


        }

        public IObservable<Packet> SendSceneRequest(IConnection peer, byte sceneId, ushort routeId, Action<Stream> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            return Observable.Create<Packet>(observer =>
            {
                var request = ReserveRequestSlot(observer);

                peer.SendToScene(sceneId, routeId, s =>
                {
                    var data = BitConverter.GetBytes(request.id);
                    s.Write(data, 0, data.Length);
                    writer(s);

                }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE, (char)0);
                return () =>
                {
                    _pendingRequests.TryRemove(request.id, out request);
                };
            });
        }

        private Request ReserveRequestSlot(IObserver<Packet> observer)
        {
            ushort id = 0;
            while (id < ushort.MaxValue)
            {
                if (!_pendingRequests.ContainsKey(id))
                {
                    var request = new Request { lastRefresh = DateTime.UtcNow, id = id, observer = observer };
                    if (_pendingRequests.TryAdd(id, request))
                    {
                        return request;
                    }
                }
                id++;
            }
            _logger.Error("Unable to create a new request: Too many pending requests.");
            throw new Exception("Unable to create new request: Too many pending requests.");
        }


        private Dictionary<byte, Func<RequestContext, Task>> _handlers = new Dictionary<byte, Func<RequestContext, Task>>();
        public void AddSystemRequestHandler(byte msgId, Func<RequestContext, Task> handler)
        {
            if (isRegistered)
            {
                throw new InvalidOperationException("Can only add handler before 'RegisterProcessor' is called.");
            }
            _handlers.Add(msgId, handler);

        }
    }


    public class RequestContext
    {
        private Packet _packet;
        private byte[] _requestId;
        private MemoryStream _stream = new MemoryStream();
        private bool DidSentValues = false;
        public Packet Packet
        {
            get
            {
                return _packet;
            }
        }
        public RequestContext(Packet p)
        {
            this._packet = p;
            _requestId = new byte[2];
            p.Stream.Read(_requestId, 0, 2);

            p.Stream.CopyTo(_stream);
            _stream.Seek(0, SeekOrigin.Begin);
        }

        public Stream InputStream
        {
            get
            {
                return _stream;
            }
        }

        public bool IsComplete
        {
            get;
            private set;
        }
        public void Send(Action<Stream> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (IsComplete)
            {
                throw new InvalidOperationException("The request is already completed.");
            }
            this.DidSentValues = true;
            _packet.Connection.SendSystem((byte)MessageIDTypes.ID_REQUEST_RESPONSE_MSG, s =>
            {
                s.Write(_requestId, 0, 2);
                writer(s);
            });

        }


        public void Complete()
        {
            _packet.Connection.SendSystem((byte)MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, s =>
            {
                s.Write(_requestId, 0, 2);

                s.WriteByte((byte)(DidSentValues ? 1 : 0));
            });
        }
        public void Error(Action<Stream> writer)
        {

            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            _packet.Connection.SendSystem((byte)MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, s =>
            {
                s.Write(_requestId, 0, 2);
                writer(s);
            });
        }




    }
}
