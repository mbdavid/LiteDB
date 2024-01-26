using System;
using System.Diagnostics;
using System.Text;

namespace LiteDB
{
    public static class Logging
    {
        public static event Action<LogEventArgs> LogCallback;
        
        /// <summary>
        /// Log a message using Debug.WriteLine
        /// </summary>
        [DebuggerHidden]
        public static void LOG(string message, string category)
        {
            LogCallback?.Invoke(new LogEventArgs(){ Message = message, Category = category});
            //Debug.WriteLine is too slow in multi-threads
            //var threadID = Environment.CurrentManagedThreadId;
            //Debug.WriteLine(message, threadID + "|" + category);
        }

        /// <summary>
        /// Log a message using Debug.WriteLine only if conditional = true
        /// </summary>
        [DebuggerHidden]
        public static void LOG(bool conditional, string message, string category)
        {
            if (conditional) LOG(message, category);
        }

        public static void LOG(Exception exception, string category)
        {
            LogCallback?.Invoke(new LogEventArgs(){ Exception = exception, Category = category});
        }
    }
    
    public class LogEventArgs
    {
        public string Category { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}