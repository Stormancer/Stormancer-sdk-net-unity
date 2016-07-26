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
            ctx.SceneDependenciesRegistration += (IDependencyBuilder db, ISceneHost scene) =>
            {
                db.Register<RpcService>().InstancePerScene();
            };

            ctx.SceneCreated += scene =>
            {
                scene.Metadata.Add(PluginName, Version);

                var processor = scene.DependencyResolver.Resolve<RpcService>();

                // Register(processor);
                scene.AddRoute(NextRouteName, p =>
                {
                    processor.Next(p);
                    return Task.FromResult(true);
                }, o => o.AutoDisposePacketStream(false));
                scene.AddRoute(CancellationRouteName, p =>
                {
                    processor.Cancel(p);
                    return Task.FromResult(true);
                }, o => o);
                scene.AddRoute(ErrorRouteName, p =>
                {
                    processor.Error(p);
                    return Task.FromResult(true);
                }, o => o);
                scene.AddRoute(CompletedRouteName, p =>
                {
                    processor.Complete(p);
                    return Task.FromResult(true);
                }, o => o);

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
            public bool HasCompleted = false;
            public IObserver<Packet<IScenePeerClient>> Observer { get; set; }
            public TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();
        }
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<ushort, Request> _pendingRequests = new ConcurrentDictionary<ushort, Request>();
        private ConcurrentDictionary<Tuple<long, ushort>, CancellationTokenSource> _runningRequests = new ConcurrentDictionary<Tuple<long, ushort>, CancellationTokenSource>();
        private ConcurrentDictionary<long, CancellationTokenSource> _peersCts = new ConcurrentDictionary<long, CancellationTokenSource>();

        private readonly ISceneHost _scene;

        /// <summary>
        /// Creates the RPC service associated with the scene
        /// </summary>
        /// <param name="scene"></param>
        /// <remarks>Do not call this constructor, use the Dependency Resolver to get the RPC service instead.</remarks>
        public RpcService(ISceneHost scene)
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
                    //string version;
                    //if (!rr.Metadata.TryGetValue(RpcHostPlugin.PluginName, out version) || version != RpcHostPlugin.Version)
                    //{
                    //    throw new InvalidOperationException("The target remote route does not support the plugin RPC version " + RpcHostPlugin.Version);
                    //}

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

                    var cancellationToken = GetCancellationTokenForPeer(peer);

                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    linkedCts.Token.Register(() =>
                    {
                        observer.OnError(new PeerDisconnectedException("Peer disconnecter from the scene."));
                    });


                    return () =>
                    {
                        linkedCts.Dispose();
                        Request _;
                        if (!rq.HasCompleted && _pendingRequests.TryRemove(id, out _))
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
            this._scene.AddRoute(route, async p =>
             {
                 var buffer = new byte[2];
                 p.Stream.Read(buffer, 0, 2);
                 var id = BitConverter.ToUInt16(buffer, 0);
                 var peerCancellationToken = GetCancellationTokenForPeer(p.Connection);

                 var cts = CancellationTokenSource.CreateLinkedTokenSource(peerCancellationToken);

                 var ctx = new RequestContext<IScenePeerClient>(p.Connection, _scene, id, ordered, new SubStream(p.Stream, false), cts);
                 var identifier = Tuple.Create(p.Connection.Id, id);
                 if (_runningRequests.TryAdd(identifier, cts))
                 {
                     try
                     {
                         await handler.InvokeWrapping(ctx);

                         _runningRequests.TryRemove(identifier, out cts);


                         ctx.SendCompleted();


                     }
                     catch (AggregateException ae)
                     {
                         var errorSent = false;
                         var ex = ae.InnerExceptions.OfType<ClientException>();
                         if (ex.Any())
                         {
                             ctx.SendError(string.Join("|", ex.Select(e => e.Message)));
                             errorSent = true;
                         }
                         if (ae.InnerExceptions.Any(e => !(e is ClientException)))
                         {
                             string errorMessage = string.Format("An error occured while executing procedure '{0}'.", route);
                             if (!errorSent)
                             {
                                 var errorId = Guid.NewGuid().ToString("N");
                                 ctx.SendError($"An exception occurred on the server. Error {errorId}.");

                                 errorMessage = $"Error {errorId}. " + errorMessage;
                             }

                             _scene.DependencyResolver.Resolve<ILogger>().Log(LogLevel.Error, "rpc.server", errorMessage, ae);
                         }
                     }
                     finally
                     {
                         ctx.Dispose();
                     }

                 }
                 else
                 {
                     ctx.Dispose();
                 }
             }, o => o, new Dictionary<string, string> { { RpcHostPlugin.PluginName, RpcHostPlugin.Version
} });
        }

        private CancellationToken GetCancellationTokenForPeer(IScenePeerClient peer)
        {
            return _peersCts.GetOrAdd(peer.Id, _ => new CancellationTokenSource()).Token;
        }

        private ushort ReserveId()
        {
            lock (this._lock)
            {
                unchecked
                {
                    int loop = 0;
                    do
                    {
                        loop++;
                        _currentRequestId++;
                        if (loop > ushort.MaxValue)
                        {
                            throw new InvalidOperationException("Too many requests in progress, unable to start a new one.");
                        }
                    } while (_pendingRequests.ContainsKey(_currentRequestId));
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
            id = ExtractRequestId(p);

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

        private static ushort ExtractRequestId(Packet<IScenePeerClient> p)
        {
            ushort id;
            var buffer = new byte[2];
            p.Stream.Read(buffer, 0, 2);
            id = BitConverter.ToUInt16(buffer, 0);
            return id;
        }

        internal void Next(Packet<IScenePeerClient> p)
        {
            var rq = GetPendingRequest(p);
            if (rq != null)
            {
                rq.Observer.OnNext(p);
                if (!rq.Tcs.Task.IsCompleted)
                {
                    rq.Tcs.TrySetResult(true);
                }
            }
        }

        internal void Error(Packet<IScenePeerClient> p)
        {
            var id = ExtractRequestId(p);
            Request rq;
            if (_pendingRequests.TryRemove(id, out rq))
            {
                rq.HasCompleted = true;
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
                rq.HasCompleted = true;
                if (messageSent)
                {
                    rq.Tcs.Task.ContinueWith(t =>
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
            if (_runningRequests.TryGetValue(Tuple.Create(p.Connection.Id, id), out cts))
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
