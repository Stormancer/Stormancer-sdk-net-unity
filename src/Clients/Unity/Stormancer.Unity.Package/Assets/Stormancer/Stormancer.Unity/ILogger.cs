using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    //public enum LogLevel
    //{
    //    Fatal = 0,
    //    Error = 1,
    //    Warn = 2,
    //    Info = 3,
    //    Debug = 4,
    //    Trace = 5
    //}
    public interface ILogger
    {
		void Log (Stormancer.Diagnostics.LogLevel logLevel, string category, string message, object context = null);
        void Trace(string message, params object[] p);

        void Debug(string message, params object[] p);
        void Error(Exception ex);

        void Error(string format, params object[] p);
        void Info(string format, params object[] p);

    }

    public class NullLogger : ILogger
    {

        public static NullLogger Instance = new NullLogger();

		public void Log(Stormancer.Diagnostics.LogLevel logLevel, string category, string message, object context = null)
		{
			
		}

		public void Trace(string message, params object[] p)
        {

        }

        public void Error(Exception ex)
        {

        }

        public void Error(string format, params object[] p)
        {

        }

        public void Info(string format, params object[] p)
        {

        }


        public void Debug(string message, params object[] p)
        {

        }
    }

    public class DebugLogger : ILogger
    {
        private DebugLogger() { }

        public static readonly DebugLogger Instance = new DebugLogger();

		public void Log(Stormancer.Diagnostics.LogLevel logLevel, string category, string message, object context = null)
        {
            UnityEngine.Debug.Log(logLevel.ToString() + ": " + category + ": " + message);
        }

        public void Trace(string message, params object[] p)
        {
            Log(Stormancer.Diagnostics.LogLevel.Trace, "client", string.Format(message, p));
        }

        public void Debug(string message, params object[] p)
        {
			Log(Stormancer.Diagnostics.LogLevel.Debug, "client", string.Format(message, p));
        }

        public void Error(Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
        }

        public void Error(string format, params object[] p)
        {
			Log(Stormancer.Diagnostics.LogLevel.Error, "client", string.Format(format, p));
        }

        public void Info(string format, params object[] p)
        {
			Log(Stormancer.Diagnostics.LogLevel.Info, "client", string.Format(format, p));
        }
    }

}
