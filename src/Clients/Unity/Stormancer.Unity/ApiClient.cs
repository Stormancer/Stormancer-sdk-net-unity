using Http;
using Stormancer.Client45.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    internal class ApiClient
    {
        private ClientConfiguration _config;
        private const string CreateTokenUri = "{0}/{1}/scenes/{2}/token";
        private readonly ITokenHandler _tokenHandler;
        public ApiClient(ClientConfiguration configuration, ITokenHandler tokenHandler)
        {
            _config = configuration;
            _tokenHandler = tokenHandler;
        }

        public Task<SceneEndpoint> GetSceneEndpoint<T>(string accountId, string applicationName, string sceneId, T userData)
        {
            var serializer = new MsgPackSerializer();

            byte[] data;
            using (var s = new MemoryStream())
            {
                serializer.Serialize(userData, s);
                data = s.ToArray();
            }

            var uri = new Uri(_config.GetApiEndpoint(), string.Format(CreateTokenUri, accountId, applicationName, sceneId));
            var request = new Request("POST", uri.AbsoluteUri, data);
            request.AddHeader("Content-Type", "application/msgpack");

            return request.Send().ContinueWith(t =>
            {
                try
                {
                    var response = t.Result;

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HTTPException exception)
                    {
                        if (exception.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new ArgumentException("Unable to get the scene {0}/{1}/{2}. Please check you entered the correct account id, application name and scene id.", exception);
                        }
                        throw;
                    }

                    return _tokenHandler.DecodeToken(response.ReadAsString());
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("An error occured while retrieving the connection token. See the inner exception for more informations.", ex);
                }
            });
        }
    }
}
