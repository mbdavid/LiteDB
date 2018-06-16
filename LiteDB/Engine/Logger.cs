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
        public LogEntry(string level, int thread, string message)
        {
            this.Time = DateTime.Now;
            this.Level = level;
            this.Thread = thread;
            this.Message = message;
        }

        public DateTime Time { get; private set; }
        public string Level { get; private set; }
        public int Thread { get; private set; }
        public string Message { get; private set; }
    }

    /// <summary>
    /// A logger class to log all information about database. Used with levels. Level = 0 - 255
    /// All log will be trigger before operation execute (better for debug)
    /// </summary>
    public class Logger
    {
        public const byte NONE = 0;
        public const byte ERROR = 1;
        public const byte COMMAND = 2;
        public const byte QUERY = 4;
        public const byte WAL = 8;
        public const byte LOCK = 16;
        public const byte FULL = 255;

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
        /// Event when log writes a message. Fire on each log message
        /// </summary>
        public event Action<LogEntry> Logging = null;

        /// <summary>
        /// To full logger use Logger.FULL or any combination of Logger constants like Level = Logger.ERROR | Logger.COMMAND | Logger.DISK
        /// </summary>
        public byte Level { get; set; }

        public Logger()
        {
            this.Level = NONE;
        }

        internal void Error(Exception ex)
        {
            this.Write(ERROR, ex.Message);
        }

        internal void Command(string command)
        {
            this.Write(COMMAND, command);
        }

        internal void Command(string command, string name)
        {
            this.Write(COMMAND, command + $" '{name}'");
        }

        internal void Query(string collection, QueryPlan plan)
        {
            this.Write(QUERY, plan.GetExplainPlan(collection));
        }

        internal void Wal(BasePage page)
        {
        }

        internal void LockEnter(string name)
        {
            this.Write(LOCK, $"entering {name}");
        }

        internal void LockEnter(string mode, string collection)
        {
            this.Write(LOCK, $"entering {mode} lock collection '{collection}'");
        }

        internal void LockExit(string name)
        {
            this.Write(LOCK, $"exiting {name}");
        }

        internal void LockExit(string mode, string collection)
        {
            this.Write(LOCK, $"exiting {mode} lock collection '{collection}'");
        }

        /// <summary>
        /// Execute msg function only if level are enabled
        /// </summary>
        public void Write(byte level, Func<string> fn)
        {
            if ((level & this.Level) == 0) return;

            this.Write(level, fn());
        }

        /// <summary>
        /// Write log text to output using inside a component (statics const of Logger)
        /// </summary>
        public void Write(byte level, string message, params object[] args)
        {
            this.Write(level, string.Format(message, args));
        }

        /// <summary>
        /// Write log text to output using inside a component (statics const of Logger)
        /// </summary>
        public void Write(byte level, string message)
        {
            if ((level & this.Level) == 0 || string.IsNullOrEmpty(message)) return;

            if (this.Logging != null)
            {
                var str =
                    level == ERROR ? "ERROR" :
                    level == COMMAND ? "COMMAND" :
                    level == QUERY ? "QUERY" :
                    level == LOCK ? "LOCK" :
                    level == WAL ? "WAL" : "";

                var log = new LogEntry(str, Thread.CurrentThread.ManagedThreadId, message);

                try
                {
                    this.Logging(log);
                }
                catch
                {
                }
            }
        }
    }
}