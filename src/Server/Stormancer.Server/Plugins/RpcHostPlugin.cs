using Stormancer.Core;
using Stormancer.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    /// <summary>
    /// Server RPC plugin
    /// </summary>
    public class RpcHostPlugin : IHostPlugin
    {
        internal const string NextRouteName = "stormancer.rpc.next";
        internal const string ErrorRouteName = "stormancer.rpc.error";
        internal const string CompletedRouteName = "stormancer.rpc.completed";
        internal const string CancellationRouteName = "stormancer.rpc.cancel";

        internal const string Version = "1.1.0";
        internal const string PluginName = "stormancer.plugins.rpc";

        /// <summary>
        /// Registers the plugin
        /// </summary>
        /// <param name="ctx">A plugin registration context</param>
        public void Build(HostPluginBuildContext ctx)
        {
            ctx.SceneCreating += scene =>
            {
                scene.Metadata.Add(PluginName, Version);

                var processor = new RpcService(scene);
                scene.DependencyResolver.Register(processor);
                scene.AddRoute(NextRouteName, p =>
                {
                    processor.Next(p);
                });
                scene.AddRoute(CancellationRouteName, p =>
                {
                    processor.Cancel(p);
                });
                scene.AddRoute(ErrorRouteName, p =>
                {
                    processor.Error(p);
                });
                scene.AddRoute(CompletedRouteName, p =>
                {
                    processor.Complete(p);
                });

                scene.Disconnected.Add(processor.PeerDisconnected);


            };
        }
    }

    /// <summary>
    /// Used to send remote procedure call through the RPC plugin
    /// </summary>
    /// <remarks>
    /// If your scene uses the RPC plugin, use `scene.GetService&lt;RpcRequestProcessor&gt;()` to get an instance of this class.
    /// You can also use the `Scene.SendRequest` extension methods.
    /// </remarks>
    public class RpcService
    {

        private ushort _currentRequestId = 0;
        private class Request
        {
            public IObserver<Packet<IScenePeerClient>> Observer { get; set; }
            public int ReceivedMsg;
            public TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        }
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<ushort, Request> _pendingRequests = new ConcurrentDictionary<ushort, Request>();
        private ConcurrentDictionary<uint, CancellationTokenSource> _runningRequests = new ConcurrentDictionary<uint, CancellationTokenSource>();
        private ConcurrentDictionary<long, CancellationTokenSource> _peersCts = new ConcurrentDictionary<long, CancellationTokenSource>();

        private readonly ISceneHost _scene;

        internal RpcService(ISceneHost scene)
        {
            _scene = scene;
        }
        /// <summary>
        /// Starts a RPC to the scene host.
        /// </summary>
        /// <param name="route">The remote route on which the message will be sent.</param>
        /// <param name="writer">The writer used to build the request's content.</param>
        /// <param name="priority">The priority used to send the request.</param>
        /// <param name="peer">Remote peer on which to call the procedure</param>
        /// <returns>An IObservable instance returning the RPC responses.</returns>
        public IObservable<Packet<IScenePeerClient>> Rpc(string route, IScenePeerClient peer, Action<Stream> writer, PacketPriority priority)
        {

            return Observable.Create<Packet<IScenePeerClient>>(
                observer =>
                {
                    var rr = peer.Routes.FirstOrDefault(r => r.Name == route);
                    if (rr == null)
                    {
                        throw new ArgumentException("The target route does not exist on the remote host.");
                    }
                    string version;
                    if (!rr.Metadata.TryGetValue(RpcHostPlugin.PluginName, out version) || version != RpcHostPlugin.Version)
                    {
                        throw new InvalidOperationException("The target remote route does not support the plugin RPC version " + RpcHostPlugin.Version);
                    }

                    var rq = new Request { Observer = observer };
                    var id = this.ReserveId();
                    if (_pendingRequests.TryAdd(id, rq))
                    {

                        _scene.Send(new MatchPeerFilter(peer), route, s =>
                        {
                            s.Write(BitConverter.GetBytes(id), 0, 2);
                            writer(s);
                        }, priority, PacketReliability.RELIABLE_ORDERED);
                    }

                    return () =>
                    {
                        if (_pendingRequests.TryRemove(id, out rq))
                        {
                            _scene.Send(new MatchPeerFilter(peer), RpcHostPlugin.CancellationRouteName, s =>
                            {
                                s.Write(BitConverter.GetBytes(id), 0, 2);
                            }, priority, PacketReliability.RELIABLE_ORDERED);
                        }
                    };
                });
        }

        /// <summary>
        /// Number of pending RPCs.
        /// </summary>
        public ushort PendingRequests
        {
            get
            {
                return (ushort)_pendingRequests.Count;
            }
        }

        /// <summary>
        /// Adds a procedure that can be called by remote peer to the scene.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="handler"></param>
        /// <param name="ordered">True if the message should be alwayse receive in order, false otherwise.</param>
        /// <remarks>
        /// The procedure is added to the scene to which this service is attached.
        /// </remarks>
        public void AddProcedure(string route, Func<RequestContext<IScenePeerClient>, Task> handler, bool ordered)
        {
            this._scene.AddRoute(route, p =>
            {

                var buffer = new byte[2];
                p.Stream.Read(buffer, 0, 2);
                var id = BitConverter.ToUInt16(buffer, 0);

                CancellationTokenSource peerCts = _peersCts.GetOrAdd(p.Connection.Id, _ => new CancellationTokenSource());

                var cts = CancellationTokenSource.CreateLinkedTokenSource(peerCts.Token);

                var ctx = new RequestContext<IScenePeerClient>(p.Connection, _scene, id, ordered, new SubStream(p.Stream, false), cts.Token);
                if (_runningRequests.TryAdd(id, cts))
                {
                    handler(ctx).ContinueWith(t =>
                    {
                        _runningRequests.TryRemove(id, out cts);

                        if (t.IsCompleted)
                        {
                            ctx.SendCompleted();
                        }
                        else
                        {
                            var ex = t.Exception.InnerExceptions.OfType<ClientException>();
                            if (ex.Any())
                            {
                                ctx.SendError(string.Join("|", ex.Select(e => e.Message)));
                            }
                            if (t.Exception.InnerExceptions.Any(e => !(e is ClientException)))
                            {
                                _scene.DependencyResolver.Resolve<ILogger>().Log(LogLevel.Error, "rpc.server", string.Format("An error occured while executing procedure '{0}'.", route), ex);
                            }
                        }

                    });
                }
            }, new Dictionary<string, string> { { RpcHostPlugin.PluginName, RpcHostPlugin.Version } });
        }
        private ushort ReserveId()
        {
            lock (this._lock)
            {
                unchecked
                {
                    int loop = 0;
                    while (_pendingRequests.ContainsKey(_currentRequestId))
                    {
                        loop++;
                        _currentRequestId++;
                        if (loop > ushort.MaxValue)
                        {
                            throw new InvalidOperationException("Too many requests in progress, unable to start a new one.");
                        }
                    }
                    return _currentRequestId;
                }
            }
        }

        private Request GetPendingRequest(Packet<IScenePeerClient> p)
        {
            ushort id;
            return GetPendingRequest(p, out id);
        }

        private Request GetPendingRequest(Packet<IScenePeerClient> p, out ushort id)
        {
            var buffer = new byte[2];
            p.Stream.Read(buffer, 0, 2);
            id = BitConverter.ToUInt16(buffer, 0);

            Request request;
            if (_pendingRequests.TryGetValue(id, out request))
            {
                return request;
            }
            else
            {
                return null;
            }
        }
        internal void Next(Packet<IScenePeerClient> p)
        {
            var rq = GetPendingRequest(p);
            if (rq != null)
            {
                System.Threading.Interlocked.Increment(ref rq.ReceivedMsg);
                rq.Observer.OnNext(p);
                if (!rq.tcs.Task.IsCompleted)
                {
                    rq.tcs.TrySetResult(true);
                }
            }
        }

        internal void Error(Packet<IScenePeerClient> p)
        {
            var rq = GetPendingRequest(p);
            if (rq != null)
            {
                rq.Observer.OnError(new ClientException(p.ReadObject<string>()));
            }
        }

        internal void Complete(Packet<IScenePeerClient> p)
        {
            var messageSent = p.Stream.ReadByte() != 0;
            ushort id;
            var rq = GetPendingRequest(p, out id);
            Request _;
            if (rq != null)
            {
                if (messageSent)
                {
                    rq.tcs.Task.ContinueWith(t =>
                    {
                        _pendingRequests.TryRemove(id, out _);
                        rq.Observer.OnCompleted();
                    });
                }
                else
                {
                    _pendingRequests.TryRemove(id, out _);
                    rq.Observer.OnCompleted();
                }
            }
        }

        internal void Cancel(Packet<IScenePeerClient> p)
        {
            var buffer = new byte[2];
            p.Stream.Read(buffer, 0, 2);
            var id = BitConverter.ToUInt16(buffer, 0);
            CancellationTokenSource cts;
            if (_runningRequests.TryGetValue(id, out cts))
            {
                cts.Cancel();
            }
        }

        internal Task PeerDisconnected(DisconnectedArgs arg)
        {
            CancellationTokenSource cts;
            if (_peersCts.TryRemove(arg.Peer.Id, out cts))
            {
                cts.Cancel();
            }
            return Task.FromResult(true);
        }
    }
}
