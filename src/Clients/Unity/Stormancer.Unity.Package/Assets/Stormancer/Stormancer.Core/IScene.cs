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
        /// Gets a component registered in the scene for a type
        /// </summary>
        /// <typeparam name="T">The requested type.</typeparam>
        /// <returns>The component registered for the type `T`, null if it doesn't exist.</returns>
        T GetComponent<T>();

        /// <summary>
        /// Registers a component for the scene.
        /// </summary>
        /// <typeparam name="T">The type the component should be registered for.</typeparam>
        /// <param name="component">The component's factory.</param>
        void RegisterComponent<T>(Func<T> component);
    }

   
}
