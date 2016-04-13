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
       
        private readonly StormancerResolver _resolver;
        public ApiClient(ClientConfiguration configuration, StormancerResolver resolver)
        {
            _config = configuration;
            _resolver = resolver;
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
            var logger = _resolver.Resolve<ILogger>();
            logger.Log(Stormancer.Diagnostics.LogLevel.Trace, "Client", "creating endpoint request for remote server");
            var uri = new Uri(_config.GetApiEndpoint(), string.Format(CreateTokenUri, accountId, applicationName, sceneId));
            var request = new Request("POST", uri.AbsoluteUri, data);
            request.AddHeader("Content-Type", "application/msgpack");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("x-version", "1.0.0");
            logger.Trace("Sending endpoint request to remote server");


            return SendWithRetry(request, 5000, 15000).ContinueWith(t =>
            {
                logger.Log(Stormancer.Diagnostics.LogLevel.Trace, "Client", "Received endpoint response from remote server");
                try
                {
                    var response = t.Result;

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HTTPException exception)
                    {
                        logger.Log(Stormancer.Diagnostics.LogLevel.Error, "Client", "GetScene failed.");
                        if (exception.StatusCode == HttpStatusCode.NotFound)
                        {
                            logger.Log(Stormancer.Diagnostics.LogLevel.Error, "Client", "GetScene failed: Unable to get the scene. Please check you entered the correct account id, application name and scene id.");
                            throw new ArgumentException("Unable to get the scene {0}/{1}/{2}. Please check you entered the correct account id, application name and scene id.", exception);
                        }
                        throw;
                    }

                    logger.Log(Stormancer.Diagnostics.LogLevel.Trace, "Client", "Token succefully received");
                    return _resolver.Resolve<ITokenHandler>().DecodeToken(response.ReadAsString());
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    logger.Log(Stormancer.Diagnostics.LogLevel.Error, "Client", "GetScene failed: cannot retreive the connection token.");
                    throw new InvalidOperationException("An error occured while retrieving the connection token. See the inner exception for more informations.", ex);
                }
            });
        }

        private Task<IResponse> SendWithRetry(Request request, int firstTry, int secondTry)
        {
            var logger = _resolver.Resolve<ILogger>();

            return request.Send().TimeOut(firstTry)
                .ContinueWith(t1 =>
                {
                    if (t1.IsCanceled)
                    {
                        logger.Debug("First call to API timed out.");
                        return request.Send().TimeOut(secondTry)
                            .ContinueWith(t2 =>
                            {
                                if (t2.IsCanceled)
                                {
                                    logger.Debug("Second call to API timed out.");
                                    return request.Send().TimeOut(secondTry * 2);
                                }
                                else
                                {
                                    return t2;
                                }
                            }).Unwrap();
                    }
                    else
                    {
                        return t1;
                    }
                }).Unwrap();
        }
    }
}
