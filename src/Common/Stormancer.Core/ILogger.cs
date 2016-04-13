using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;

namespace Stormancer.Diagnostics
{
    /// <summary>
    /// The available log levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Applies to critical errors that prevent the program from continuing.
        /// </summary>
        Fatal,
        /// <summary>
        /// Applies to non critical errors
        /// </summary>
        Error,
        /// <summary>
        /// Applies to warnings
        /// </summary>
        Warn,
        /// <summary>
        /// Applies to information messages about the execution of the application
        /// </summary>
        Info,
        /// <summary>
        /// Applies to detailed informations useful when debugging
        /// </summary>
        Debug,
        /// <summary>
        /// Applies to very detailed informations about the execution
        /// </summary>
        Trace
    }
    /// <summary>
    /// Contract for a Logger in Stormancer.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a json message
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="category">The category of the message.</param>
        /// <param name="message">Log message</param>
        /// <param name="data">Additionnal data about the log.</param>
        void Log(LogLevel level, string category, string message, object data);

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="level"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        void Log(LogLevel level, string category, string message, Exception ex);
    }

    /// <summary>
    /// Extensions for the ILogger class.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a step.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IDisposable Step(this ILogger logger, string message)
        {
            var id = Guid.NewGuid().ToString();
            logger.Log(LogLevel.Trace, "step.start", message, new { id = id, time = DateTime.UtcNow });
            var disposable = new DisposableAction(() =>
            {
                logger.Log(LogLevel.Trace, "step.complete", message, new { id = id, time = DateTime.UtcNow });
            });
            return disposable;
        }

        /// <summary>
        /// Writes a trace event to the log.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Trace(this ILogger logger, string category, string message, params object[] args)
        {
            logger.Log(LogLevel.Trace, category, string.Format(message, args), new { });
        }

        /// <summary>
        /// Writes a debug event to the log.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Debug(this ILogger logger, string category, string message, params object[] args)
        {
            logger.Log(LogLevel.Debug, category, string.Format(message, args), new { });
        }

        /// <summary>
        /// Writes a warn event to the log.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Warn(this ILogger logger, string category, string message, params object[] args)
        {
            logger.Log(LogLevel.Warn, category, string.Format(message, args), new { });
        }
        /// <summary>
        /// Writes a Info event to the log.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Info(this ILogger logger, string category, string message, params object[] args)
        {
            logger.Log(LogLevel.Info, category, string.Format(message, args), new { });
        }

        /// <summary>
        /// Writes an error event to the log.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Error(this ILogger logger, string category, string message, params object[] args)
        {
            logger.Log(LogLevel.Error, category, string.Format(message, args), new { });
        }
    }

    /// <summary>
    /// A logger implementation that does nothing.
    /// </summary>
    public class NullLogger : ILogger
    {
        private NullLogger() { }
        private static ILogger _instance = new NullLogger();

        /// <summary>
        /// Retrieves a NullLogger instance
        /// </summary>
        public static ILogger Instance
        {
            get
            {
                return _instance;
            }
        }
        /// <summary>
        /// Writes a log entry
        /// </summary>
        /// <param name="level"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public void Log(LogLevel level, string category, string message, object data)
        {

        }

        /// <summary>
        /// Writes a log entry using an exception
        /// </summary>
        /// <param name="level"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public void Log(LogLevel level, string category, string message, Exception ex)
        {
            
        }
    }
}
