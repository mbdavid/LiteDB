using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class that execute QueryPlan returing results
    /// </summary>
    internal class QueryExecutor
    {
        private readonly LiteEngine _engine;
        private readonly TransactionMonitor _monitor;
        private readonly SortDisk _sortDisk;
        private readonly bool _utcDate;
        private readonly string _collection;
        private readonly Query _query;
        private readonly IEnumerable<BsonDocument> _source;

        public QueryExecutor(LiteEngine engine, TransactionMonitor monitor, SortDisk sortDisk, bool utcDate, string collection, Query query, IEnumerable<BsonDocument> source)
        {
            _engine = engine;
            _monitor = monitor;
            _sortDisk = sortDisk;
            _utcDate = utcDate;
            _collection = collection;
            _query = query;

            LOG(_query.ToSQL(_collection).Replace(Environment.NewLine, " "), "QUERY");

            // source will be != null when query will run over external data source, like system collections or files (not user collection)
            _source = source;
        }

        public BsonDataReader ExecuteQuery()
        {
            if (_query.Into == null)
            {
                return this.ExecuteQuery(_query.ExplainPlan);
            }
            else
            {
                return this.ExecuteQueryInto(_query.Into, _query.IntoAutoId);
            }
        }

        /// <summary>
        /// Run query definition into engine. Execute optimization to get query planner
        /// </summary>
        internal BsonDataReader ExecuteQuery(bool executionPlan)
        {
            var transaction = _monitor.GetTransaction(true, out var isNew);

            transaction.OpenCursors++;

            try
            {
                // encapsulate all execution to catch any error
                return new BsonDataReader(RunQuery(), _collection);
            }
            catch
            {
                // if any error, rollback transaction
                transaction.Rollback();
                throw;
            }

            IEnumerable<BsonDocument> RunQuery()
            {
                var snapshot = transaction.CreateSnapshot(_query.ForUpdate ? LockMode.Write : LockMode.Read, _collection, false);

                // no collection, no documents
                if (snapshot.CollectionPage == null && _source == null)
                {
                    if (--transaction.OpenCursors == 0 && transaction.ExplicitTransaction == false)
                    {
                        transaction.Commit();
                    }

                    // if query use Source (*) need runs with empty data source
                    if (_query.Select.UseSource)
                    {
                        yield return _query.Select.ExecuteScalar().AsDocument;
                    }

                    yield break;
                }

                // execute optimization before run query (will fill missing _query properties instance)
                var optimizer = new QueryOptimization(snapshot, _query, _source);

                var queryPlan = optimizer.ProcessQuery();

                // if execution is just to get explan plan, return as single document result
                if (executionPlan)
                {
                    yield return queryPlan.GetExecutionPlan();

                    if (--transaction.OpenCursors == 0 && transaction.ExplicitTransaction == false)
                    {
                        transaction.Commit();
                    }

                    yield break;
                }

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = queryPlan.Index.Run(snapshot.CollectionPage, new IndexService(snapshot));

                // get current query pipe: normal or groupby pipe
                using (var pipe = queryPlan.GetPipe(transaction, snapshot, _sortDisk, _utcDate))
                {
                    // commit transaction before close pipe
                    pipe.Disposing += (s, e) =>
                    {
                        if (--transaction.OpenCursors == 0 && transaction.ExplicitTransaction == false)
                        {
                            transaction.Commit();
                        }
                    };

                    // call safepoint just before return each document
                    foreach (var doc in pipe.Pipe(nodes, queryPlan))
                    {
                        yield return doc;
                    }
                }
            };
        }

        /// <summary>
        /// Execute query and insert result into another collection. Support external collections
        /// </summary>
        internal BsonDataReader ExecuteQueryInto(string into, BsonAutoId autoId)
        {
            IEnumerable<BsonDocument> getResultset()
            {
                using (var reader = this.ExecuteQuery(false))
                {
                    while(reader.Read())
                    {
                        yield return reader.Current.AsDocument;
                    }
                }
            }

            var result = 0;

            // if collection starts with $ it's system collection
            if (into.StartsWith("$"))
            {
                SqlParser.ParseCollection(new Tokenizer(into), out var name, out var options);
            
                var sys = _engine.GetSystemCollection(name);
            
                result = sys.Output(getResultset(), options);
            }
            // otherwise insert as normal collection
            else
            {
                result = _engine.Insert(into, getResultset(), autoId);
            }

            return new BsonDataReader(result);
        }
    }
}