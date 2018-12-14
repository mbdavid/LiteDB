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
        private readonly StringBuilder _summary = new StringBuilder();
        private readonly Stopwatch _time = new Stopwatch();

        public string Summary => _summary.ToString();

        public bool Result { get; private set; }

        public DatabaseReport()
        {
            this.Result = true;

            _summary.AppendLine("LiteDB Check Integrity Report");
            _summary.AppendLine("=============================");
        }

        internal void Run(string title, string ok, Func<object> action)
        {
            _summary.Append(title.PadRight(28, '.') + ": ");

            try
            {
                var result = action();
                _summary.AppendLine(string.Format(ok, result));
            }
            catch (Exception ex)
            {
                this.Result = false;

                _summary.AppendLine("ERR: " + ex.Message);
            }
        }

        public override string ToString()
        {
            return _summary.ToString();
        }
    }
}