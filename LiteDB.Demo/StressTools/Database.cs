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
    public class Database
    {
        private readonly string _taskName;
        private readonly LiteDatabase _db;
        private readonly Logger _logger;
        private readonly Stopwatch _watch;
        private readonly ConcurrentCounter _concurrent;

        private long _delay;

        public int Index { get; }

        public Database(string taskName, LiteDatabase db, Logger logger, Stopwatch watch, ConcurrentCounter concurrent, int index)
        {
            _taskName = taskName;
            _db = db;
            _logger = logger;
            _watch = watch;
            _concurrent = concurrent;

            _delay = watch.ElapsedMilliseconds;

            this.Index = index;
        }

        public BsonValue ExecuteScalar(string sql, params BsonValue[] args)
        {
            return this.Execute(sql, args).FirstOrDefault();
        }

        public BsonValue[] Query(string sql, params BsonValue[] args)
        {
            return this.Execute(sql, args);
        }

        public int Insert(string collection, BsonDocument document, string autoId = "int")
        {
            return this.Execute($"INSERT INTO {collection}:{autoId} VALUES {JsonSerializer.Serialize(document)}").FirstOrDefault().AsInt32;
        }

        private BsonValue[] Execute(string sql, params BsonValue[] args)
        {
            var parameters = new BsonDocument();

            for (var i = 0; i < args.Length; i++)
            {
                parameters[i.ToString()] = args[i];
            }

            var log = new Log
            {
                Task = _taskName,
                Timer = (int)_watch.ElapsedMilliseconds,
                Concurrent = _concurrent.Increment() - 1,
                Delay = (int)(_watch.ElapsedMilliseconds - _delay),
                Sql = sql,
                Params = parameters
            };

            var start = DateTime.Now;

            try
            {
                return _db.Execute(sql, parameters).ToArray();
            }
            catch(Exception ex)
            {
                log.Error = ex.Message + '\n' + ex.StackTrace;

                throw ex;
            }
            finally
            {
                _concurrent.Decrement();

                log.Elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;

                _logger?.Insert(log);

                _delay = _watch.ElapsedMilliseconds;
            }
        }
    }
}
