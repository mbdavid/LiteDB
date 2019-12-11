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
                Command = sql,
                Thread = Task.CurrentId
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

        public int Insert(string collection, BsonDocument document, BsonAutoId autoId = BsonAutoId.Int32)
        {
            return this.Insert(collection, new BsonDocument[] { document }, autoId);
        }

        public int Insert(string collection, IEnumerable<BsonDocument> documents, BsonAutoId autoId = BsonAutoId.Int32)
        {
            var log = new Log
            {
                Task = _taskName,
                Timer = (int)_watch.ElapsedMilliseconds,
                Concurrent = _concurrent.Increment() - 1,
                Delay = (int)(_watch.ElapsedMilliseconds - _delay),
                Command = "INSERT " + collection,
                Thread = Task.CurrentId
            };

            var start = DateTime.Now;

            try
            {
                return _db.GetCollection(collection, autoId).Insert(documents);
            }
            catch (Exception ex)
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
