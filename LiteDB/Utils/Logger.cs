using System;

namespace LiteDB
{
    /// <summary>
    /// A logger class to log all information about database. Used with levels. Level = 0 - 255
    /// All log will be trigger before operation execute (better for debug)
    /// </summary>
    public class Logger
    {
        public const byte NONE = 0;
        public const byte ERROR = 1;
        public const byte RECOVERY = 2;
        public const byte COMMAND = 4;
        public const byte LOCK = 8;
        public const byte QUERY = 16;
        public const byte JOURNAL = 32;
        public const byte CACHE = 64;
        public const byte DISK = 128;
        public const byte FULL = 255;

        /// <summary>
        /// Initialize logger class using a custom logging level (see Logger.NONE to Logger.FULL)
        /// </summary>
        public Logger(byte level = NONE, Action<string> logging = null)
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
        /// To full logger use Logger.FULL or any combination of Logger constants like Level = Logger.ERROR | Logger.COMMAND | Logger.DISK
        /// </summary>
        public byte Level { get; set; }

        public Logger()
        {
            this.Level = NONE;
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
        public void Write(byte level, string message)
        {
            if (Check(level))
                _Write(level, message, null);
        }

        public void Write<T>(byte level, string message, T arg)
        {
            if (Check(level))
                _Write(level, message, new object[] { arg });
        }

        public void Write<T1, T2>(byte level, string message, T1 arg1, T2 arg2)
        {
            if (Check(level))
                _Write(level, message, new object[] { arg1, arg2 });
        }

        public void Write<T1, T2, T3>(byte level, string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (Check(level))
                _Write(level, message, new object[] { arg1, arg2, arg3 });
        }

        private bool Check(byte level)
        {
            return (level & this.Level) != 0 && this.Logging != null;
        }

        private void _Write(byte level, string message, object[] args)
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

            try {
                this.Logging(msg);
            } catch {
            }
        }
    }
}