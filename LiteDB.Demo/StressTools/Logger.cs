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
        private readonly LiteRepository _db;
        private List<Log> _cache = new List<Log>();
        private string _logName;

        public Logger(string connectionString)
        {
            //_db = new LiteRepository(connectionString);
        }

        public void Initialize(string name)
        {
            //_db.Database.DropCollection(name);
            //
            //_db.Database.Checkpoint();

            _logName = name;
        }

        public void Insert(Log log)
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

            //_db.Insert((IEnumerable<Log>)logs, _logName);
        }

        public void Dispose()
        {
            this.Flush();

            //_db.Dispose();
        }
    }
}
