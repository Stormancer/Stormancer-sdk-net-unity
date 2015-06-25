using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Stormancer.Networking;
using Stormancer.Core;
using Stormancer.Networking.Messages;
using Stormancer.Diagnostics;


namespace Stormancer.Networking.Processors
{

    /// <summary>
    /// Processes system requests
    /// </summary>
    public class RequestProcessor : IPacketProcessor
    {
        private class Request
        {
            public DateTime lastRefresh;
            public ushort id;
            //public IObserver<Packet> observer;
            public TaskCompletionSource<Packet> tcs;
        }
        private readonly ConcurrentDictionary<ushort, Request> _pendingRequests;
        private readonly ILogger _logger;

        private bool _isRegistered = false;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="modules"></param>
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

        /// <summary>
        /// Registers the processor into the dispatcher
        /// </summary>
        /// <param name="config"></param>
        public void RegisterProcessor(PacketProcessorConfig config)
        {
            _isRegistered = true;

            foreach (var handler in _handlers) //Add system request handlers
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
                                context.Error(s => p.Serializer().Serialize(msg, s));
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
                if (_pendingRequests.TryRemove(id, out request))
                {
                    p.Metadata["request"] = request;
                    request.lastRefresh = DateTime.UtcNow;
                    request.tcs.TrySetResult(p);
                }
                else
                {
                    _logger.Trace("requestProcessor", "Unknown request id.");
                }

                return true;
            });

            config.AddProcessor((byte)MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, p =>
            {
                var temp = new byte[2];
                p.Stream.Read(temp, 0, 2);
                var id = BitConverter.ToUInt16(temp, 0);
                var hasValues = p.Stream.ReadByte() == 1;
                if (!hasValues)
                {
                    Request request;
                    if (this._pendingRequests.TryRemove(id, out request))
                    {
                        p.Metadata["request"] = request;
                        request.tcs.TrySetResult(null);
                    }
                    else
                    {
                        _logger.Trace("requestProcessor", "Unknown request id.");
                    }
                }

                return true;
            });

            config.AddProcessor((byte)MessageIDTypes.ID_REQUEST_RESPONSE_ERROR, p =>
            {
                var temp = new byte[2];
                p.Stream.Read(temp, 0, 2);
                var id = BitConverter.ToUInt16(temp, 0);

                Request request;
                if (_pendingRequests.TryRemove(id, out request))
                {
                    p.Metadata["request"] = request;

                    var msg = p.Serializer().Deserialize<string>(p.Stream);
                    request.tcs.TrySetException(new ClientException(msg));
                }
                else
                {
                    _logger.Trace("requestProcessor", "Unknown request id.");
                }

                return true;
            });
        }

        /// <summary>
        /// Sends a system request to the remote peer
        /// </summary>
        /// <param name="peer">A target peer</param>
        /// <param name="msgId">Message id</param>
        /// <param name="writer">An action writing the request parameters</param>
        /// <param name="priority">Priority of the request.</param>
        /// <returns>An observable returning the request responses</returns>
        public Task<Packet> SendSystemRequest(IConnection peer, byte msgId, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            var tcs = new TaskCompletionSource<Packet>();
            var request = ReserveRequestSlot(tcs);

            peer.SendSystem((byte)MessageIDTypes.ID_SYSTEM_REQUEST, bs =>
            {
                var bw = new BinaryWriter(bs);
                bw.Write(msgId);
                bw.Write(request.id);
                bw.Flush();
                writer(bs);

            },priority);

            return tcs.Task;
        }
        private Request ReserveRequestSlot(TaskCompletionSource<Packet> tcs)
        {
            ushort id = 0;
            while (id < ushort.MaxValue)
            {
                if (!_pendingRequests.ContainsKey(id))
                {
                    var request = new Request { lastRefresh = DateTime.UtcNow, id = id, tcs = tcs };
                    if (_pendingRequests.TryAdd(id, request))
                    {
                        return request;
                    }
                }
                id++;
            }
            _logger.Error("requestProcessor", "Unable to create a new request: Too many pending requests.");
            throw new Exception("Unable to create new request: Too many pending requests.");
        }


        private Dictionary<byte, Func<RequestContext, Task>> _handlers = new Dictionary<byte, Func<RequestContext, Task>>();

        /// <summary>
        /// Register a new system request handlers for the specified message Id
        /// </summary>
        /// <param name="msgId">System message id</param>
        /// <param name="handler">A function that handles message with the provided id</param>
        public void AddSystemRequestHandler(byte msgId, Func<RequestContext, Task> handler)
        {
            if (_isRegistered)
            {
                throw new InvalidOperationException("Can only add handler before 'RegisterProcessor' is called.");
            }
            _handlers.Add(msgId, handler);

        }
    }

    /// <summary>
    /// System request context
    /// </summary>
    public class RequestContext
    {
        private Packet _packet;
        private byte[] _requestId;
        private MemoryStream _stream = new MemoryStream();
        private bool DidSentValues = false;

        /// <summary>
        /// Packet that initiated the request
        /// </summary>
        public Packet Packet
        {
            get
            {
                return _packet;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p"></param>
        public RequestContext(Packet p)
        {
            this._packet = p;
            _requestId = new byte[2];
            p.Stream.Read(_requestId, 0, 2);

            p.Stream.CopyTo(_stream);
            _stream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Stream exposing the request parameters
        /// </summary>
        public Stream InputStream
        {
            get
            {
                return _stream;
            }
        }

        /// <summary>
        /// Is the request complete?
        /// </summary>
        public bool IsComplete
        {
            get;
            private set;
        }

        /// <summary>
        /// Sends a response to the request
        /// </summary>
        /// <param name="writer"></param>
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

        /// <summary>
        /// Completes the system request
        /// </summary>
        public void Complete()
        {
            _packet.Connection.SendSystem((byte)MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, s =>
            {
                s.Write(_requestId, 0, 2);

                s.WriteByte((byte)(DidSentValues ? 1 : 0));
            });
        }

        /// <summary>
        /// Completes the system request with an error
        /// </summary>
        /// <param name="writer">Action writing the error</param>
        public void Error(Action<Stream> writer)
        {

            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            _packet.Connection.SendSystem((byte)MessageIDTypes.ID_REQUEST_RESPONSE_ERROR, s =>
            {
                s.Write(_requestId, 0, 2);
                writer(s);
            });
        }




    }
}
