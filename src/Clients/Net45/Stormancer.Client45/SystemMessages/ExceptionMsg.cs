using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Networking.Messages
{
    /// <summary>
    /// Dto representing an exception
    /// </summary>
    public class ExceptionMsg
    {
        /// <summary>
        /// Creates an empty ExceptionMsg object
        /// </summary>
        public ExceptionMsg() { }

        /// <summary>
        /// Creates an ExceptionMsg object from an Exception
        /// </summary>
        /// <param name="ex"></param>
        public ExceptionMsg(Exception ex)
        {
            if(ex == null)
            {
                Message = "Null object was passed as an exception.";
                return;
            }
            Type = ex.GetType().Name;
            Message = ex.Message;
            var st = new StackTrace(ex, true);
            var stArray = new StackFrame[st.FrameCount];

            for (int i = 0; i < st.FrameCount; i++)
            {
                var frame = st.GetFrame(i);
                stArray[i] = new StackFrame { Method = frame.GetMethod().DeclaringType.ToString()+":"+frame.GetMethod().ToString(), Column = frame.GetFileColumnNumber(), File = frame.GetFileName(), Line = frame.GetFileLineNumber() };
            }
            StackTrace = stArray;
            var aggrEx = ex as AggregateException;
            if (aggrEx != null)
            {
                this.InnerExceptions = aggrEx.InnerExceptions.Select(e => new ExceptionMsg(e)).ToArray();
            }
            else if (ex.InnerException != null)
            {
                this.InnerExceptions = new[] { new ExceptionMsg(ex.InnerException) };
            }
            else
            {
                this.InnerExceptions = new ExceptionMsg[0];
            }
        }

        /// <summary>
        /// Type of the exception
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Stack trace
        /// </summary>
        public StackFrame[] StackTrace { get; set; }

        /// <summary>
        /// Inner exceptions
        /// </summary>
        public ExceptionMsg[] InnerExceptions { get; set; }
    }

    /// <summary>
    /// Dto representing a stack frame in `ExceptionMsg` objects
    /// </summary>
    public class StackFrame
    {
        /// <summary>
        /// Method name
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Line in the source code
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column in the source code
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Name of the file containing the source code
        /// </summary>
        public string File { get; set; }
    }
}
