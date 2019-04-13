using System;

namespace LiteDB
{
    using static LoggerLevel;

    [Flags]
    public enum LoggerLevel : byte
    {
        NONE = 0, 
        ERROR = 1,
        RECOVERY = 2,
        COMMAND = 4,
        LOCK = 8,
        QUERY = 16,
        JOURNAL = 32,
        CACHE = 64,
        DISK = 128,
        FULL = 255
    }

    /// <summary>
    /// A logger class to log all information about database. Used with levels. Level = 0 - 255
    /// All log will be trigger before operation execute (better for debug)
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Initialize logger class using a custom logging level (see Logger.NONE to Logger.FULL)
        /// </summary>
        public Logger(LoggerLevel level = NONE, Action<string> logging = null)
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
        public event Action<string> Logging = null;

        /// <summary>
        /// To full logger use Logger.FULL or any combination of Logger constants like Level = LoggerLevel.ERROR | LoggerLevel.COMMAND | LoggerLevel.DISK
        /// </summary>
        public LoggerLevel Level { get; set; }

        public Logger()
        {
            this.Level = NONE;
        }

        /// <summary>
        /// Execute msg function only if level are enabled
        /// </summary>
        public void Write(LoggerLevel level, Func<string> fn)
        {
            if ((level & this.Level) == 0) return;

            this.Write(level, fn());
        }

        /// <summary>
        /// Write log text to output using inside a component (statics const of Logger)
        /// </summary>
        public void Write(LoggerLevel level, string message, params object[] args)
        {
            if ((level & this.Level) == 0 || string.IsNullOrEmpty(message)) return;

            if (this.Logging != null)
            {
                var text = string.Format(message, args);

                var str =
                    level == ERROR ? "ERROR" :
                    level == RECOVERY ? "RECOVERY" :
                    level == COMMAND ? "COMMAND" :
                    level == JOURNAL ? "JOURNAL" :
                    level == LOCK ? "LOCK" :
                    level == QUERY ? "QUERY" :
                    level == CACHE ? "CACHE" : 
                    level == DISK ? "DISK" : "";

                var msg = DateTime.Now.ToString("HH:mm:ss.ffff") + " [" + str + "] " + text;

                try
                {
                    this.Logging(msg);
                }
                catch
                {
                }
            }
        }
    }
}