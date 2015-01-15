using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Cluster.Application
{


    public class ConnectionData
    {
        public Dictionary<string, string> Endpoints { get; set; }

        public string AccountId { get; set; }
        public string Application { get; set; }

        public string SceneId { get; set; }

        public string Routing { get; set; }

        public DateTime Issued { get; set; }

        public DateTime Expiration { get; set; }

        public byte[] UserData { get; set; }

        public string ContentType { get; set; }
    }
}
