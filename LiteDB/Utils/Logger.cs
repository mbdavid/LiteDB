using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// A logger class to log all information about database. Used with levels. Level = 0 - 255 
    /// </summary>
    public class Logger
    {
        public const byte NONE = 0;
        public const byte ERROR = 1;
        public const byte COMMAND = 2;
        public const byte RECOVERY = 4;
        public const byte QUERY = 8;
        public const byte INDEX = 16;
        public const byte JOURNAL = 32;
        public const byte DISK = 64;
        public const byte CACHE = 128;
        public const byte FULL = 255;

        public Stopwatch DiskRead = new Stopwatch();
        public Stopwatch DiskWrite = new Stopwatch();
        public Stopwatch Serialize = new Stopwatch();
        public Stopwatch Deserialize = new Stopwatch();

        /// <summary>
        /// To full logger use Logger.FULL or any combination of Logger constants
        /// </summary>
        public byte Level { get; set; }

        public Action<string> WriteLine = (text) =>
        {
            var aux = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(text);
            Console.ForegroundColor = aux;
        };

        public Logger()
        {
            this.Level = NONE;
        }

        public void Reset()
        {
        }

        public void Error(string format, params object[] args)
        {
            this.Write(ERROR, string.Format(format, args));
        }

        public void Debug(byte component, string format, params object[] args)
        {
            this.Write(component, string.Format(format, args));
        }

        internal void Write(byte component, string text)
        {
            if((component & this.Level) == 0) return;

            var comp =
                component == ERROR ? "ERROR" :
                component == COMMAND ? "COMMAND" :
                component == RECOVERY ? "RECOVERY" :
                component == QUERY ? "QUERY" :
                component == INDEX ? "INDEX" :
                component == JOURNAL ? "JOURNAL" :
                component == DISK ? "DISK" : "CACHE";

            var msg = string.Format("{0:HH:mm:ss.ffff} [{1}] {2}",
                DateTime.Now,
                comp,
                text);

            WriteLine(msg);
        }

    }
}
