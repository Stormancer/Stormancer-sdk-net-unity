using Stormancer.Networking.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Stormancer.Networking
{
    /// <summary>
    /// Handles system requests
    /// </summary>
    public interface IRequestModule
    {
        void Register(RequestModuleBuilder builder);
    }

    /// <summary>
    /// Object passed to the IRequestModule.Register method to allow modules to add handlers to system requests.
    /// </summary>
    public class RequestModuleBuilder
    {
        private readonly Action<byte,Func<RequestContext,Task>> _addHandler;
        internal RequestModuleBuilder(Action<byte,Func<RequestContext,Task>> addHandler)
        {
            if(addHandler == null)
            {
                throw new ArgumentNullException("addHandler");
            }
            _addHandler = addHandler;
        }

        /// <summary>
        /// Adds a request handler for the specified message Id.
        /// </summary>
        /// <param name="msgId">A byte representing the message id of the requests to handle.</param>
        /// <param name="handler">A `Func&lt;RequestContext,Task&gt;` object handling the requests for the provided message id.  </param>
        public void Service(byte msgId,Func<RequestContext,Task> handler)
        {
            _addHandler(msgId, handler);
        }
    }
}
