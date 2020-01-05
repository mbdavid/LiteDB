using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class Logger : IDisposable
    {
        private readonly string _filename;
        private readonly List<LogItem> _cache = new List<LogItem>();

        public Logger(string filename)
        {
        }

        public void Insert(LogItem log)
        {
            lock(_cache)
            {
                _cache.Add(log);

                if (_cache.Count > 1000) this.Flush();
            }
        }

        private void Flush()
        {
            var logs = _cache.ToArray();

            _cache.Clear();

            Task.Factory.StartNew(() =>
            {
                //_db.Insert((IEnumerable<Log>)logs, "EventLog");
            });
        }

        public void Dispose()
        {
            this.Flush();
        }
    }

    public class LogItem
    {
        public DateTime Date { get; } = DateTime.Now;
        public string Task { get; set; }
        public int Index { get; set; }
        public int? Thread { get; set; }
        public double Elapsed { get; set; }
        public int Concurrent { get; set; }
        public string Command { get; set; }
        public string Error { get; set; }
    }
}
