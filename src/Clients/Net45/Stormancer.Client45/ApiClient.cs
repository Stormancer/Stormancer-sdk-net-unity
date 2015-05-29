using Newtonsoft.Json;
using Stormancer.Client45.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    internal class ApiClient
    {
        private ClientConfiguration _config;
        private const string CreateTokenUri = "{0}/{1}/scenes/{2}/token";
        private readonly ITokenHandler _tokenHandler;
        private readonly MsgPackSerializer _serializer = new MsgPackSerializer();

        public ApiClient(ClientConfiguration configuration, ITokenHandler tokenHandler)
        {
            _config = configuration;
            _tokenHandler = tokenHandler;
        }

        private HttpClient CreateHttpClient()
        {
            var result = new HttpClient();

            result.BaseAddress = _config.GetApiEndpoint();
            result.DefaultRequestHeaders.Add("x-version", Constants.Version);

            return result;
        }

        public async Task<SceneEndpoint> GetSceneEndpoint<T>(string accountId, string applicationName, string sceneId, T userData)
        {
            using (var client = CreateHttpClient())
            {

                try
                {
                    using (var stream = new MemoryStream())
                    {
                        _serializer.Serialize(userData, stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        var content = new StreamContent(stream);
                        var result = await client.PostAsync(string.Format(CreateTokenUri, accountId, applicationName, sceneId), content);


                        result.EnsureSuccessStatusCode();


                        return _tokenHandler.DecodeToken(await result.Content.ReadAsStringAsync());
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("An error occured while retrieving the connection token. See the inner exception for more informations.", ex);
                }
            }
        }
    }
}
