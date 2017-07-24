using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins.GameVersion
{
    public class GameVersionService : IDisposable
    {
        private IDisposable _gameVersionRegistration;
        private IDisposable _serverVersionRegistration;

        public GameVersionService(Scene scene)
        {
            GameVersion = "unknown";

            _gameVersionRegistration = scene.AddRoute<string>("gameVersion.update", version =>
            {
                GameVersion = version;
                OnGameVersionUpdate(version);
            });

            _serverVersionRegistration = scene.AddRoute<string>("serverVersion.update", version =>
            {
                var callback = OnServerVersionUpdate;
                if (callback != null)
                {
                    callback(version);
                }
            });

            var tcs = new TaskCompletionSource<string>();
            Action<string> firstGameVersionUpdate = null;

            firstGameVersionUpdate = version =>
            {
                OnGameVersionUpdate += v => GameVersionTask = TaskHelper.FromResult(v);
                OnGameVersionUpdate -= firstGameVersionUpdate;
                tcs.SetResult(version);
            };
            GameVersionTask = tcs.Task;
            OnGameVersionUpdate += firstGameVersionUpdate;
        }

        public string GameVersion { get; private set; }
        public Task<string> GameVersionTask { get; private set; }

        public event Action<string> OnGameVersionUpdate;
        public event Action<string> OnServerVersionUpdate;

        #region IDisposable Support
        private bool disposedValue = false; // Pour détecter les appels redondants

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_gameVersionRegistration != null)
                    {
                        _gameVersionRegistration.Dispose();
                        _gameVersionRegistration = null;
                    }
                    if (_serverVersionRegistration != null)
                    {
                        _serverVersionRegistration.Dispose();
                        _serverVersionRegistration = null;
                    }
                }

                // TODO: libérer les ressources non managées (objets non managés) et remplacer un finaliseur ci-dessous.
                // TODO: définir les champs de grande taille avec la valeur Null.

                disposedValue = true;
            }
        }

        // TODO: remplacer un finaliseur seulement si la fonction Dispose(bool disposing) ci-dessus a du code pour libérer les ressources non managées.
        // ~GameVersionService() {
        //   // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
        //   Dispose(false);
        // }

        // Ce code est ajouté pour implémenter correctement le modèle supprimable.
        void IDisposable.Dispose()
        {
            // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
            Dispose(true);
            // TODO: supprimer les marques de commentaire pour la ligne suivante si le finaliseur est remplacé ci-dessus.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
