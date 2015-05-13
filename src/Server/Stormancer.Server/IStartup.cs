using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Represents a startup class for server applications
    /// </summary>
    /// <remarks>
    /// Your server application startup classes don't need to implement this interface : The host looks for appropriate startup classes
    /// through duck typing. This interface however can help you with autocompletion.
    /// </remarks>
    public interface IStartup
    {
        /// <summary>
        /// Startup method of your server application.
        /// </summary>
        /// <param name="builder">Buider object that allow the application to initialize itself and interact with the host</param>
        void Run(IAppBuilder builder);
    }
}
