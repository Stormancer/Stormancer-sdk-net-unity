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

            ctx.SceneCreated += (ISceneHost scene) =>
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


                scene.AddInternalRoute(NextRouteName, async p =>
                {
                    processor.Next(p);
                    await Task.FromResult(true);
                }, o => o.AutoDisposePacketStream(false));
                scene.AddInternalRoute(CancellationRouteName, p =>
                {
                    processor.Cancel(p);
                    return Task.FromResult(true);
                }, o => o);
                scene.AddInternalRoute(ErrorRouteName, p =>
                {
                    processor.Error(p);
                    return Task.FromResult(true);
                }, o => o);
                scene.AddInternalRoute(CompletedRouteName, p =>
                {
                    processor.Complete(p);
                    return Task.FromResult(true);
                }, o => o);

                scene.Disconnected.Add(processor.PeerDisconnected);
                scene.Shuttingdown.Add(processor.SceneShuttingDown);


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
        private class Request<T> where T : IScenePeer
        {
            public bool HasCompleted = false;
            public IObserver<Packet<T>> Observer { get; set; }
            public TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();
        }
        private class RunningRequest<T> where T : IScenePeer
        {
            public RequestContext<T> ctx;
            public CancellationTokenSource cts;
        }

        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<ushort, Request<IScenePeerClient>> _pendingRequests = new ConcurrentDictionary<ushort, Request<IScenePeerClient>>();
        private readonly ConcurrentDictionary<ushort, Request<IScenePeer>> _pendingInternalRequests = new ConcurrentDictionary<ushort, Request<IScenePeer>>();


        private ConcurrentDictionary<Tuple<long, ushort>, CancellationTokenSource> _runningRequests = new ConcurrentDictionary<Tuple<long, ushort>, CancellationTokenSource>();
        private ConcurrentDictionary<Tuple<string, uint, ushort>, RunningRequest<IScenePeer>> _runningInternalRequests = new ConcurrentDictionary<Tuple<string, uint, ushort>, RunningRequest<IScenePeer>>();

        private ConcurrentDictionary<long, CancellationTokenSource> _peersCts = new ConcurrentDictionary<long, CancellationTokenSource>();
        private ConcurrentDictionary<string, CancellationTokenSource> _sceneCts = new ConcurrentDictionary<string, CancellationTokenSource>();

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
        /// Starts a RPC to the provided peer.
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

                    var rq = new Request<IScenePeerClient> { Observer = observer };
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
                        Request<IScenePeerClient> _;
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
        /// Starts a RPC to another scene. Use AddInternalProcedure to handle RPC called by this method
        /// </summary>
        /// <param name="route">The remote route on which the message will be sent.</param>
        /// <param name="writer">The writer used to build the request's content.</param>
        /// <param name="priority">The priority used to send the request.</param>
        /// <param name="filter">Filter that describes the target scene</param>
        /// <returns>An IObservable instance returning the RPC responses.</returns>
        public IObservable<Packet<IScenePeer>> Rpc(string route, MatchSceneFilter filter, Action<Stream> writer, PacketPriority priority)
        {

            return Observable.Create<Packet<IScenePeer>>(
                observer =>
                {
                   
                    //string version;
                    //if (!rr.Metadata.TryGetValue(RpcHostPlugin.PluginName, out version) || version != RpcHostPlugin.Version)
                    //{
                    //    throw new InvalidOperationException("The target remote route does not support the plugin RPC version " + RpcHostPlugin.Version);
                    //}

                    var rq = new Request<IScenePeer> { Observer = observer };
                    var id = this.ReserveId();
                    if (_pendingInternalRequests.TryAdd(id, rq))
                    {

                        _scene.Send(filter, route, s =>
                        {
                            s.Write(BitConverter.GetBytes(id), 0, 2);
                            writer(s);
                        }, priority, PacketReliability.RELIABLE_ORDERED);
                    }

                    //var cancellationToken = GetCancellationTokenForPeer(peer);

                    //var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    //linkedCts.Token.Register(() =>
                    //{
                    //    observer.OnError(new PeerDisconnectedException("Peer disconnecter from the scene."));
                    //});


                    return () =>
                    {
                       
                        Request<IScenePeer> _;
                        if (!rq.HasCompleted && _pendingInternalRequests.TryRemove(id, out _))
                        {
                            _scene.Send(filter, RpcHostPlugin.CancellationRouteName, s =>
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
        /// Adds a procedure which can be called from another scene host.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="handler"></param>
        /// <param name="ordered"></param>
        public void AddInternalProcedure(string route, Func<RequestContext<IScenePeer>, Task> handler, bool ordered)
        {
            this._scene.AddInternalRoute(route, async p =>
            {
                var buffer = new byte[2];
                p.Stream.Read(buffer, 0, 2);
                var id = BitConverter.ToUInt16(buffer, 0);
                var peerCancellationToken = GetCancellationTokenForScene(p.Connection);

                var cts = CancellationTokenSource.CreateLinkedTokenSource(peerCancellationToken);

                var ctx = new RequestContext<IScenePeer>(p.Connection, _scene, id, ordered, new SubStream(p.Stream, false), cts);
                var identifier = Tuple.Create(p.Connection.SceneId, p.Connection.ShardId, id);
                var rq = new RunningRequest<IScenePeer> { ctx = ctx, cts = cts };
                if (_runningInternalRequests.TryAdd(identifier,rq))
                {
                    try
                    {
                        Exception e = null;
                        bool errorOccured = false;
                        try
                        {
                            await handler.InvokeWrapping(ctx);
                        }
                        catch (Exception ex)
                        {
                            errorOccured = true;
                            e = ex;
                        }


                        if (!errorOccured)
                        {

                            ctx.SendCompleted();


                        }
                        else
                        {
                            var errorSent = false;
                            var ex = e as ClientException;
                            if (ex != null)
                            {
                                ctx.SendError(string.Join("|", ex.Message));
                                errorSent = true;
                            }
                            else
                            {
                                string errorMessage = string.Format("An error occured while executing procedure '{0}'.", route);
                                if (!errorSent)
                                {
                                    var errorId = Guid.NewGuid().ToString("N");
                                    ctx.SendError($"An exception occurred on the server. Error {errorId}.");

                                    errorMessage = $"Error {errorId}. " + errorMessage;
                                }

                                _scene.DependencyResolver.Resolve<ILogger>().Log(LogLevel.Error, "rpc.host.server", errorMessage, e);
                            }
                        }
                    }
                    finally
                    {
                        _runningInternalRequests.TryRemove(identifier, out rq);
                        ctx.Dispose();
                    }

                }
                else
                {
                    ctx.Dispose();
                }
            }, o => o);
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
                         Exception e = null;
                         bool errorOccured = false;
                         try
                         {
                             await handler.InvokeWrapping(ctx);
                         }
                         catch (Exception ex)
                         {
                             errorOccured = true;
                             e = ex;
                         }


                         if (!errorOccured)
                         {

                             ctx.SendCompleted();


                         }
                         else
                         {
                             var errorSent = false;
                             var ex = e as ClientException;
                             if (ex != null)
                             {
                                 ctx.SendError(string.Join("|", ex.Message));
                                 errorSent = true;
                             }
                             else
                             {
                                 string errorMessage = string.Format("An error occured while executing procedure '{0}'.", route);
                                 if (!errorSent)
                                 {
                                     var errorId = Guid.NewGuid().ToString("N");
                                     ctx.SendError($"An exception occurred on the server. Error {errorId}.");

                                     errorMessage = $"Error {errorId}. " + errorMessage;
                                 }

                                 _scene.DependencyResolver.Resolve<ILogger>().Log(LogLevel.Error, "rpc.client.server", errorMessage, e);
                             }
                         }
                     }
                     finally
                     {
                         _runningRequests.TryRemove(identifier, out cts);
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

        private CancellationToken GetCancellationTokenForScene(IScenePeer peer)
        {
            return _sceneCts.GetOrAdd($"{peer.SceneId}/{peer.ShardId}", _ => new CancellationTokenSource()).Token;
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

        private Request<IScenePeerClient> GetPendingRequest(Packet<IScenePeerClient> p)
        {
            ushort id;
            return GetPendingRequest(p, out id);
        }
        private Request<IScenePeer> GetPendingRequest(Packet<IScenePeer> p)
        {
            ushort id;
            return GetPendingRequest(p, out id);
        }

        private Request<IScenePeerClient> GetPendingRequest(Packet<IScenePeerClient> p, out ushort id)
        {
            id = ExtractRequestId(p);


            Request<IScenePeerClient> request;
            if (_pendingRequests.TryGetValue(id, out request))
            {
                return request;
            }
            else
            {
                return null;
            }

        }
        private Request<IScenePeer> GetPendingRequest(Packet<IScenePeer> p, out ushort id)
        {
            id = ExtractRequestId(p);


            Request<IScenePeer> request;
            if (_pendingInternalRequests.TryGetValue(id, out request))
            {
                return request;
            }
            else
            {
                return null;
            }

        }

        private static ushort ExtractRequestId<T>(Packet<T> p)
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

        internal void Next(Packet<IScenePeer> p)
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
            Request<IScenePeerClient> rq;
            if (_pendingRequests.TryRemove(id, out rq))
            {
                rq.HasCompleted = true;
                rq.Observer.OnError(new ClientException(p.ReadObject<string>()));
            }
        }
        internal void Error(Packet<IScenePeer> p)
        {
            var id = ExtractRequestId(p);
            Request<IScenePeer> rq;
            if (_pendingInternalRequests.TryRemove(id, out rq))
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
            Request<IScenePeerClient> _;
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

        internal void Complete(Packet<IScenePeer> p)
        {
            var messageSent = p.Stream.ReadByte() != 0;
            ushort id;
            var rq = GetPendingRequest(p, out id);
            Request<IScenePeer> _;
            if (rq != null)
            {
                rq.HasCompleted = true;
                if (messageSent)
                {
                    rq.Tcs.Task.ContinueWith(t =>
                    {
                        _pendingInternalRequests.TryRemove(id, out _);
                        rq.Observer.OnCompleted();
                    });
                }
                else
                {
                    _pendingInternalRequests.TryRemove(id, out _);
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

        internal void Cancel(Packet<IScenePeer> p)
        {
            var buffer = new byte[2];
            p.Stream.Read(buffer, 0, 2);
            var id = BitConverter.ToUInt16(buffer, 0);
            RunningRequest<IScenePeer> ctx;
            if (_runningInternalRequests.TryGetValue(Tuple.Create(p.Connection.SceneId, p.Connection.ShardId, id), out ctx))
            {
                ctx.cts.Cancel();
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

        internal async Task SceneShuttingDown(ShutdownArgs arg)
        {
            foreach (var rq in _runningInternalRequests.Values.ToArray())
            {
                rq.ctx.SendError("Scene shutting down");
            }
            await Task.FromResult(true);

        }
    }
}
