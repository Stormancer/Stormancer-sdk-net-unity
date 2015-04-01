using Newtonsoft.Json;
using Stormancer.Cluster.Application;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Client45.Infrastructure
{
    internal interface ITokenHandler
    {
        SceneEndpoint DecodeToken(string token);
    }

    internal class TokenHandler:ITokenHandler
    {
        private readonly ISerializer _tokenSerializer;

        public TokenHandler()
        {
            _tokenSerializer = new MsgPackSerializer();
        }
        public SceneEndpoint DecodeToken(string token)
        {
            token = token.Trim('"');
            var data = token.Split('-')[0];
            var buffer= Convert.FromBase64String(data);

            var result = _tokenSerializer.Deserialize<ConnectionData>(new MemoryStream(buffer));

            return new SceneEndpoint { Token = token, TokenData = result };
        }
    }
}
