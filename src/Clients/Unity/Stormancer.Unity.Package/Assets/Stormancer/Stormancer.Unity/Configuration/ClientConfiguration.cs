using Stormancer.Client45.Infrastructure;
using Stormancer.Diagnostics;
using Stormancer.Networking;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.EditorPlugin;


namespace Stormancer
{
    /// <summary>
    /// Configuration object for a Stormancer client.
    /// </summary>
    /// <remarks>
    /// Client configurations objects are often built using the FromAccount static method. The resulting object can be further customized afterwards.<br/>
    /// For instance to target a custom Stormancer cluster change the ServerEndoint property to the http API endpoint of your custom cluster.
    /// </remarks>
    public class ClientConfiguration
    {
        //private const string Api = "http://localhost:23469/";
        private const string ApiEndpoint = "https://api.stormancer.com/";

        private const string LocalDevEndpoint = "http://localhost:42001/";

        /// <summary>
        /// A boolean value indicating if the client should try to connect to the local dev platform.
        /// </summary>
        public bool IsLocalDev { get; private set; }

        /// <summary>
        /// A string containing the target server endpoint.
        /// </summary>
        /// <remarks>
        /// This value overrides the *IsLocalDev* property.
        /// </remarks>
        public List<string> ServerEndpoints { get; set; }

        /// <summary>
        /// A string containing the account name of the application.
        /// </summary>
        public string Account { get; private set; }

        /// <summary>
        /// A string containing the name of the application.
        /// </summary>
        public string Application { get; private set; }

        /// <summary>
        /// Enable or disable the asynchrounous dispatch of received messages.
        /// </summary>
        /// <remarks>
        /// Asynchronous dispatch is enabled by default.
        /// </remarks>
        public bool AsynchrounousDispatch { get; set; }

        /// <summary>
        /// The interval between successive ping requests, in milliseconds
        /// </summary>
        public int PingInterval { get; set; }


        private bool _started = false;
        private int _index;
        internal Uri GetApiEndpoint()
        {
            if (!ServerEndpoints.Any())
            {

                if (IsLocalDev)
                {
                    return new Uri(LocalDevEndpoint);
                }
                else
                {
                    return new Uri(ApiEndpoint);
                }
            }

            if (!_started)
            {
                var rand = new Random();
                _index = rand.Next() % ServerEndpoints.Count;
                _started = true;
            }

            var result = new Uri(ServerEndpoints[_index % ServerEndpoints.Count]);
            _index++;
            return result;
        }


        /// <summary>
        /// Creates a ClientConfiguration object targeting the public online platform.
        /// </summary>
        /// <param name="account">Id of the target account</param>
        /// <param name="application">Name of the application the client will connect to.</param>
        /// <returns>A ClientConfiguration instance that enables connection to the application. The configuration can be modified afterwards.</returns>
        public static ClientConfiguration ForAccount(string account, string application)
        {
            return new ClientConfiguration { Account = account, Application = application, IsLocalDev = false };
        }

        internal Dictionary<string, string> _metadata = new Dictionary<string, string>();

        private ClientConfiguration()
        {
            Scheduler = new Stormancer.Infrastructure.DefaultScheduler();
            Logger = NullLogger.Instance;
            Dispatcher = new DefaultPacketDispatcher(new Lazy<bool>(() => this.AsynchrounousDispatch));
            TransportFactory = DefaultTransportFactory;
            //Transport = new WebSocketClientTransport(NullLogger.Instance);        

            Serializers = new List<ISerializer> { new MsgPackSerializer() };
            MaxPeers = 20;
            Plugins = new List<IClientPlugin>();
            Plugins.Add(new RpcClientPlugin());
#if UNITY_EDITOR
            Plugins.Add(new StormancerEditorPlugin());
#endif
            AsynchrounousDispatch = true;
            PingInterval = 5000;

            try
            {
                MainThread.Initialize();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("You must create a new ClientConfiguration in the Unity Main Thread.", ex);
            }
        }

        private RakNetTransport DefaultTransportFactory(IDependencyResolver DependencyResolver)
        {
            return new RakNetTransport(DependencyResolver.Resolve<ILogger>(), DependencyResolver.Resolve<IConnectionHandler>());
        }

        /// <summary>
        /// Adds metadata to connections created by the client.
        /// </summary>
        /// <param name="key">A string containing the metadata key.</param>
        /// <param name="value">A string containing the metadata value.</param>
        /// <returns>The current configuration</returns>
        /// <remarks>The metadata you provides here will be available on the server to customize its behavior.</remarks>
        ClientConfiguration Metadata(string key, string value)
        {
            _metadata[key] = value;
            return this;
        }

        /// <summary>
        /// Gets or Sets the dispatcher to be used by the client.
        /// </summary>
        public IPacketDispatcher Dispatcher { get; set; }

        /// <summary>
        /// Gets or sets the transport to be used by the client.
        /// </summary>
        public Func<IDependencyResolver, ITransport> TransportFactory { get; set; }


        /// <summary>
        /// List of available serializers for the client.
        /// </summary>
        /// <remarks>
        /// When negotiating which serializer should be used for a given remote peer, the first compatible serializer in the list is the one prefered.
        /// </remarks>
        public List<ISerializer> Serializers { get; private set; }

        /// <summary>
        /// Maximum number of remote peers that can connect with this client.
        /// </summary>
        public ushort MaxPeers { get; set; }

        public ILogger Logger { get; set; }

        /// <summary>
        /// Adds a plugin to the client.
        /// </summary>
        /// <param name="plugin">The plugin instance to add.</param>
        /// <remarks>
        /// Plugins enable developpers to plug custom code in the Stormancer client's extensibility points. Possible uses include: custom high level protocols, logger or analyzers.
        /// 
        /// </remarks>
        void AddPlugin(IClientPlugin plugin)
        {

            Plugins.Add(plugin);
        }

        internal List<IClientPlugin> Plugins { get; private set; }

        /// <summary>
        /// The scheduler used by the client to run the transport and other repeated tasks.
        /// </summary>
        public IScheduler Scheduler
        {
            get;
            set;
        }
    }
}
