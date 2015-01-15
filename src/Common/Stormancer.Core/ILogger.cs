using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Diagnostics
{
    public enum LogLevel
    {
        Fatal,
        Error,
        Warning,
        Info,
        Debug,
        Trace
    }
    public interface ILogger
    {
        void Log(LogLevel level, string category, string json);
    }
}
