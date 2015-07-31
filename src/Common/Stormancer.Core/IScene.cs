using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Represents a Stormancer scene.
    /// </summary>
    /// <remarks>
    /// In a Stormancer application, users connect to scenes to interact with each other and the application. 
    /// A scene has 2 faces: A scene host, currently only serverside scene hosts are supported, and scene clients.
    /// </remarks>
    public interface IScene
    {
        string Id { get; }

        /// <summary>
        /// True if the instance is an host. False if it's a client.
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// Dependency resolver of the scene
        /// </summary>
         IDependencyResolver DependencyResolver { get; }

        /// <summary>
        /// retrieves a component in the dependency resolver
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetComponent<T>() where T : class;
    }

   
}
