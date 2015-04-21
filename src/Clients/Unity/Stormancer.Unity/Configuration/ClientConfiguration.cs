using Stormancer.Client45.Infrastructure;
using Stormancer.Networking;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Stormancer
{
    /// <summary>
    /// Represents the configuration of a Stormancer client.
    /// </summary>
    public class ClientConfiguration
    {
        //private const string Api = "http://localhost:23469/";
        private const string ApiEndpoint = "https://api1.stormancer.com/";

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
        public string ServerEndpoint { get; set; }

        /// <summary>
        /// A string containing the account name of the application.
        /// </summary>
        public string Account { get; private set; }

        /// <summary>
        /// A string containing the name of the application.
        /// </summary>
        public string Application { get; private set; }

        internal Uri GetApiEndpoint()
        {
            if (IsLocalDev)
            {
                return new Uri(ServerEndpoint ?? LocalDevEndpoint);
            }
            else
            {
                return new Uri(ServerEndpoint ?? ApiEndpoint);
            }
        }

        /// <summary>
        /// Creates a ClientConfiguration object targeting the local development server.
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ClientConfiguration ForLocalDev(string application)
        {
            return new ClientConfiguration { Account = "local", Application = application, IsLocalDev = true };
        }

        /// <summary>
        /// Creates a ClientConfiguration object targeting the public online platform.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ClientConfiguration ForAccount(string account, string application)
        {
            return new ClientConfiguration { Account = account, Application = application, IsLocalDev = false };
        }

        internal Dictionary<string, string> _metadata = new Dictionary<string, string>();

        private ClientConfiguration()
        {
            Dispatcher = new DefaultPacketDispatcher();
            Transport = new RaknetTransport(NullLogger.Instance);
            Serializers = new List<ISerializer> { new MsgPackSerializer() };
            MaxPeers = 20;

            Plugins = new List<IClientPlugin>();
            Plugins.Add(new RpcClientPlugin());
        }

        /// <summary>
        /// Adds metadata to the connection.
        /// </summary>
        /// <param name="key">A string containing the metadata key.</param>
        /// <param name="value">A string containing the metadata value.</param>
        /// <returns></returns>
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
        public ITransport Transport { get; set; }

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

        /// <summary>
        /// Adds a plugin to the client.
        /// </summary>
        /// <param name="plugin">The plugin to add.</param>
        void AddPlugin(IClientPlugin plugin)
        {

            Plugins.Add(plugin);
        }

        internal List<IClientPlugin> Plugins { get; private set; }
    }
}
