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
    public class SqlDB
    {
        private readonly string _taskName;
        private readonly LiteDatabase _db;
        private readonly Logger _logger;
        private readonly Stopwatch _watch;
        private readonly ConcurrentCounter _concurrent;

        private long _delay;

        public SqlDB(string taskName, LiteDatabase db, Logger logger, Stopwatch watch, ConcurrentCounter concurrent)
        {
            _taskName = taskName;
            _db = db;
            _logger = logger;
            _watch = watch;
            _concurrent = concurrent;

            _delay = watch.ElapsedMilliseconds;
        }

        public BsonValue[] Execute(string sql, params BsonValue[] args)
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
                Concurrent = _concurrent.Increment(),
                Delay = (int)(_watch.ElapsedMilliseconds - _delay),
                Sql = sql,
                Params = parameters
            };

            var start = DateTime.Now;

            try
            {
                using (var reader = _db.Execute(sql, parameters))
                {
                    return reader.ToArray();
                }
            }
            catch(Exception ex)
            {
                log.Error = ex.Message;

                throw ex;
            }
            finally
            {
                _concurrent.Decrement();

                log.Elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;

                _logger.Insert(log);

                _delay = _watch.ElapsedMilliseconds;
            }

        }
    }
}
