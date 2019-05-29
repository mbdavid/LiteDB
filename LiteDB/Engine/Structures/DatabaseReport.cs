using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LiteDB.Engine
{
    /// <summary>
    /// Database report for check integrity
    /// </summary>
    public class DatabaseReport
    {
        private readonly List<string> _summary = new List<string>();
        private readonly Stopwatch _time = new Stopwatch();

        public string[] Summary => _summary.ToArray();

        public bool Result { get; private set; }

        public DatabaseReport()
        {
            this.Result = true;

            _summary.Add("LiteDB Check Integrity Report");
            _summary.Add("=============================");
        }

        internal void Run(string title, string ok, Func<object> action)
        {
            var text = title.PadRight(28, '.') + ": ";

            try
            {
                var result = action();
                text += string.Format(ok, result);
            }
            catch (Exception ex)
            {
                this.Result = false;

                text += "ERR: " + ex.Message;
            }

            _summary.Add(text);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _summary);
        }
    }
}