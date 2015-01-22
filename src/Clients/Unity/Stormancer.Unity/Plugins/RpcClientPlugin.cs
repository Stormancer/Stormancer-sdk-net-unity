using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    class RpcClientPlugin: IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += scene => {
                var rpcParams = scene.GetHostMetadata("stormancer.plugins.rpc");

                if(!string.IsNullOrEmpty(rpcParams))
                {
                    scene.AddRoute("stormancer.rpc.next", p => { });
                    scene.AddRoute("stormancer.rpc.error", p => { });
                    scene.AddRoute("stormancer.rpc.complete", p => { });
                    
                }
            };
        }


        public class RpcRequestManager
        {
            //public IObservable<Packet<IScenePeer>> SendRequest()
        }
    }
}
