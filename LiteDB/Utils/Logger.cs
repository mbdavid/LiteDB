using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a single log entry
    /// </summary>
    public class LogEntry
    {
        public LogEntry(string message)
        {
            this.Time = DateTime.Now;
            this.ThreadID = Thread.CurrentThread.ManagedThreadId;

            this.Level = "INFO";
            this.Message = message;
        }

        public LogEntry(Exception ex)
        {
            this.Time = DateTime.Now;
            this.ThreadID = Thread.CurrentThread.ManagedThreadId;
            this.Level = "ERROR";

            this.Message = ex.Message;
            this.StackTrace = ex.StackTrace;
        }

        public DateTime Time { get; private set; }
        public string Level { get; private set; }
        public int ThreadID { get; private set; }
        public string Message { get; private set; }
        public string StackTrace { get; set; }
    }

    /// <summary>
    /// Logger class to store important information about database running. Log errors and some important
    /// information only. Can
    /// </summary>
    public class Logger
    {
        public const byte NONE = 0;
        public const byte ERROR = 1;
        public const byte INFO = 2;
        public const byte FULL = 255;

        public event Action<LogEntry> Logging;

        public Logger()
        {
            this.Level = NONE;
        }

        /// <summary>
        /// Initialize logger class using a custom logging level (see Logger.NONE to Logger.FULL)
        /// </summary>
        public Logger(byte level = NONE, Action<LogEntry> logging = null)
        {
            this.Level = level;

            if (logging != null)
            {
                this.Logging += logging;
            }
        }

        /// <summary>
        /// To full logger use Logger.FULL or any combination of Logger constants like Level = Logger.ERROR | Logger.COMMAND | Logger.DISK
        /// </summary>
        public byte Level { get; set; }

        /// <summary>
        /// Log any database error excption before send error to user
        /// </summary>
        public void Error(Exception ex)
        {
            this.DoLog(ERROR, new LogEntry(ex));
        }

        /// <summary>
        /// Log database information about important facts
        /// </summary>
        public void Info(string message)
        {
            this.DoLog(INFO, new LogEntry(message));
        }

        private void DoLog(byte level, LogEntry log)
        {
            if ((level & this.Level) == 0) return;

            try
            {
                //TODO: call logging event using async Task
                this.Logging?.Invoke(log);
            }
            catch
            {
            }
        }
    }
}