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
        public const string DISK = "DISK";
        public const string JOURNAL = "JOURNAL";
        public const string RECOVERY = "RECOVERY";
        public const string COMMAND = "COMMAND";
        public const string INDEX = "INDEX";
        public const string QUERY = "QUERY";

        public Stopwatch DiskRead = new Stopwatch();
        public Stopwatch DiskWrite = new Stopwatch();
        public Stopwatch Serialize = new Stopwatch();
        public Stopwatch Deserialize = new Stopwatch();

        public TextWriter Output = Console.Out;

        public bool Enabled = false;

        public void Reset()
        {
        }

        internal void Debug(string category, string format, params object[] args)
        {
        }

        internal void Info(string category, string format, params object[] args)
        {
        }

        internal void Timer(string format, params object[] args)
        {
        }

        internal void Error(string category, Exception ex, string caller)
        {
        }

        internal void Error(string category, string text)
        {
        }

    }
}
