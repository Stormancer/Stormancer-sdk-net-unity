using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stormancer.Core;

namespace Stormancer.Server
{
    /// <summary>
    /// Represents the application host
    /// </summary>
    public interface IHost
    {
        /// <summary>
        ///  Adds a new scene template
        /// </summary>
        /// <remarks>
        /// This method can only be called before host startup has completed. The runtime assumes that this method is always called in all nodes 
        /// to guarantee that any node can create a scene from the template if required.
        /// </remarks>
        /// <param name="templateName">Name of the template</param>
        /// <param name="factory">Template factory</param>
        void AddSceneTemplate(string templateName, Action<ISceneHost> factory);

        /// <summary>
        /// Creates a scene with the provided factory
        /// </summary>
        /// <remarks>
        /// This method can only be called during host startup. The runtime assumes all nodes are running the scene.
        /// </remarks>
        /// <param name="name">Name of the scene</param>
        /// <param name="factory">Scene factory</param>
        Task CreateScene(string name, Action<ISceneHost> factory);

        /// <summary>
        /// Enumerates the scenes running on the host
        /// </summary>
        /// <returns></returns>
        IEnumerable<ISceneHost> EnumerateScenes();

        /// <summary>
        /// Registers an admin API in the application
        /// </summary>
        /// <param name="apiFactory">Factory method for the API</param>
        void RegisterAdminApiFactory(Action<Owin.IAppBuilder> apiFactory);

        /// <summary>
        /// Registers a public API Owin handler
        /// </summary>
        /// <param name="apiFactory"></param>
        void RegisterPublicApiFactory(Action<Owin.IAppBuilder> apiFactory);

        /// <summary>
        /// Application metadata
        /// </summary>
        IDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Dependency resolver
        /// </summary>
        IDependencyResolver DependencyResolver { get; }
    }
}