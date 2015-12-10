using System;

namespace LiteDB
{
    /// <summary>
    /// A logger class to log all information about database. Used with levels. Level = 0 - 255
    /// All log will be trigger before operation execute (better for log)
    /// </summary>
    public class Logger
    {
        public const byte NONE = 0;
        public const byte ERROR = 1;
        public const byte RECOVERY = 2;
        public const byte COMMAND = 4;
        public const byte QUERY = 16;
        public const byte JOURNAL = 32;
        public const byte DISK = 64;
        public const byte FULL = 255;

        /// <summary>
        /// To full logger use Logger.FULL or any combination of Logger constants like Level = Logger.ERROR | Logger.COMMAND | Logger.DISK
        /// </summary>
        public byte Level { get; set; }

        /// <summary>
        /// Output function to write log - default is log in console
        /// </summary>
        public Action<string> Output = (text) =>
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(text);
        };

        public Logger()
        {
            this.Level = NONE;
        }

        /// <summary>
        /// Write log text to output using inside a component (statics const of Logger)
        /// </summary>
        public void Write(byte component, string message, params object[] args)
        {
            if ((component & this.Level) == 0) return;

            var text = string.Format(message, args);

            var comp =
                component == ERROR ? "ERROR" :
                component == RECOVERY ? "RECOVERY" :
                component == COMMAND ? "COMMAND" :
                component == JOURNAL ? "JOURNAL" :
                component == DISK ? "DISK" : "QUERY";

            var msg = DateTime.Now.ToString("HH:mm:ss.ffff") + " [" + comp + "] " + text;

            this.Output(msg);
        }
    }
}