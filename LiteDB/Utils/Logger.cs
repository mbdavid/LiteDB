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
    /// A internal class to log/debug intenal commands
    /// </summary>
    public class Logger
    {
        public const byte COMMAND = 2;
        public const byte RECOVERY = 4;
        public const byte QUERY = 8;
        public const byte INDEX = 16;
        public const byte JOURNAL = 32;
        public const byte DISK = 64;
        public const byte CACHE = 128;

        public const byte ERROR = 1;
        public const byte INFO  = 2;
        public const byte DEBUG = 4;

        public const byte NONE     = 0;
        public const byte FULL     = 255;

        public Stopwatch DiskRead = new Stopwatch();
        public Stopwatch DiskWrite = new Stopwatch();
        public Stopwatch Serialize = new Stopwatch();
        public Stopwatch Deserialize = new Stopwatch();

        public byte Component { get; set; }
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
            this.Component = FULL;
            this.Level = NONE;
        }

        public void Reset()
        {
        }

        internal void Error(byte component, string format, params object[] args)
        {
            this.Write('E', component, string.Format(format, args));
        }

        internal void Info(byte component, string format, params object[] args)
        {
            this.Write('I', component, string.Format(format, args));
        }

        internal void Debug(byte component, string format, params object[] args)
        {
            this.Write('D', component, string.Format(format, args));
        }

        internal void Write(char severity, byte component, string text)
        {
            if((severity & this.Level) == 0 || (component & this.Component) == 0) return;

            var comp =
                component == COMMAND ? "COMMAND" :
                component == RECOVERY ? "RECOVERY" :
                component == QUERY ? "QUERY" :
                component == INDEX ? "INDEX" :
                component == JOURNAL ? "JOURNAL" :
                component == DISK ? "DISK" : "CACHE";

            var msg = string.Format("{0:HH:mm:ss.ffff} {1} [{2}] {3}",
                DateTime.Now,
                severity,
                comp,
                text);

            WriteLine(msg);
        }

    }
}
