using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class DbContext
    {
        private readonly string _taskName;
        private readonly int _index;
        private readonly LiteDatabase _db;
        private readonly Logger _logger;
        private readonly Stopwatch _watch;
        private readonly ConcurrentCounter _concurrent;

        public DbContext(string taskName, int index, LiteDatabase db, Logger logger, Stopwatch watch, ConcurrentCounter concurrent)
        {
            _taskName = taskName;
            _index = index;
            _db = db;
            _logger = logger;
            _watch = watch;
            _concurrent = concurrent;
        }

        public BsonValue Execute(string sql, params BsonValue[] args)
        {
            return this.Query(sql, args).FirstOrDefault();
        }

        public BsonValue[] Query(string sql, params BsonValue[] args)
        {
            var log = new LogItem
            {
                Task = _taskName,
                Index = _index,
                Concurrent = _concurrent.Increment() - 1,
                Command = sql,
                Thread = Task.CurrentId
            };

            var start = Stopwatch.StartNew();

            try
            {
                return _db.Execute(sql, args).ToArray();
            }
            catch (Exception ex)
            {
                log.Error = ex.Message + '\n' + ex.StackTrace;

                throw ex;
            }
            finally
            {
                _concurrent.Decrement();

                log.Elapsed = start.ElapsedMilliseconds;

                _logger?.Insert(log);
            }
        }

        public int Insert(string collection, BsonDocument document, BsonAutoId autoId = BsonAutoId.Int32)
        {
            return this.Insert(collection, new BsonDocument[] { document }, autoId);
        }

        public int Insert(string collection, IEnumerable<BsonDocument> documents, BsonAutoId autoId = BsonAutoId.Int32)
        {
            var log = new LogItem
            {
                Task = _taskName,
                Index = _index,
                Concurrent = _concurrent.Increment() - 1,
                Command = "INSERT " + collection,
                Thread = Task.CurrentId
            };

            var start = Stopwatch.StartNew();

            try
            {
                var count = _db.GetCollection(collection, autoId).Insert(documents);

                log.Command += $" ({count})";

                return count;
            }
            catch (Exception ex)
            {
                log.Error = ex.Message + '\n' + ex.StackTrace;

                throw ex;
            }
            finally
            {
                _concurrent.Decrement();

                log.Elapsed = start.ElapsedMilliseconds;

                _logger?.Insert(log);
            }
        }

        /// <summary>
        /// Helper method to generate sequencial documents
        /// </summary>
        public IEnumerable<BsonDocument> GetDocs(TimeSpan timer, Action<BsonDocument> modifier = null)
        {
            var end = DateTime.Now.Add(timer);
            var index = 0;

            while (end >= DateTime.Now)
            {
                var doc = new BsonDocument
                {
                    ["name"] = "John " + Guid.NewGuid(),
                    ["r"] = "myvalue",
                    ["t"] = ++index,
                    ["active"] = false
                };

                modifier?.Invoke(doc);

                yield return doc;
            }
        }

        /// <summary>
        /// Helper method to generate sequencial documents
        /// </summary>
        public IEnumerable<BsonDocument> GetDocs(int total, Action<BsonDocument> modifier = null)
        {
            for(var i = 0; i < total; i++)
            {
                var doc = new BsonDocument
                {
                    ["name"] = "John " + Guid.NewGuid(),
                    ["r"] = "myvalue",
                    ["t"] = i,
                    ["active"] = false
                };

                modifier?.Invoke(doc);

                yield return doc;
            }
        }
    }
}
