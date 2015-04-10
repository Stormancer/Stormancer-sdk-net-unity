using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Stormancer
{
    public interface ILogger
    {
        void Trace(string message, params object[] p);

        void Debug(string message, params object[] p);
        void Error(Exception ex);

        void Error(string format, params object[] p);
        void Info(string format, params object[] p);

    }

    public class NullLogger : ILogger
    {

        public static NullLogger Instance = new NullLogger();
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
}
