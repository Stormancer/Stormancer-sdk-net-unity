using System.Threading.Tasks;

namespace Stormancer.Plugins.ClientSettings
{
    public class ClientSettingsService
    {
        private Scene _scene;

        public ClientSettingsService(Scene scene)
        {
            this._scene = scene;
        }

        public Task<string> GetSettings()
        {
            return _scene.RpcTask<string>("clientsettings.getsettings");
        }
    }
}