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
        private readonly EnginePragmas _pragmas;
        private readonly CursorInfo _cursor;
        private readonly string _collection;
        private readonly Query _query;
        private readonly IEnumerable<BsonDocument> _source;

        public QueryExecutor(LiteEngine engine, TransactionMonitor monitor, SortDisk sortDisk, EnginePragmas pragmas, string collection, Query query, IEnumerable<BsonDocument> source)
        {
            _engine = engine;
            _monitor = monitor;
            _sortDisk = sortDisk;
            _pragmas = pragmas;
            _collection = collection;
            _query = query;

            _cursor = new CursorInfo(collection, query);

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
            // get current transaction (if contains a explicit transaction) or a query-only transaction
            var transaction = _monitor.GetTransaction(true, true, out var isNew);

            transaction.OpenCursors.Add(_cursor);

            // return new BsonDataReader with IEnumerable source
            return new BsonDataReader(RunQuery(), _collection);

            IEnumerable<BsonDocument> RunQuery()
            {
                var snapshot = transaction.CreateSnapshot(_query.ForUpdate ? LockMode.Write : LockMode.Read, _collection, false);

                // no collection, no documents
                if (snapshot.CollectionPage == null && _source == null)
                {
                    // if query use Source (*) need runs with empty data source
                    if (_query.Select.UseSource)
                    {
                        yield return _query.Select.ExecuteScalar(_pragmas.Collation).AsDocument;
                    }

                    transaction.OpenCursors.Remove(_cursor);

                    if (isNew)
                    {
                        _monitor.ReleaseTransaction(transaction);
                    }

                    yield break;
                }

                // execute optimization before run query (will fill missing _query properties instance)
                var optimizer = new QueryOptimization(snapshot, _query, _source, _pragmas.Collation);

                var queryPlan = optimizer.ProcessQuery();

                // if execution is just to get explan plan, return as single document result
                if (executionPlan)
                {
                    yield return queryPlan.GetExecutionPlan();

                    transaction.OpenCursors.Remove(_cursor);

                    if (isNew)
                    {
                        _monitor.ReleaseTransaction(transaction);
                    }

                    yield break;
                }

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = queryPlan.Index.Run(snapshot.CollectionPage, new IndexService(snapshot, _pragmas.Collation));

                // get current query pipe: normal or groupby pipe
                var pipe = queryPlan.GetPipe(transaction, snapshot, _sortDisk, _pragmas);

                try
                {
                    // start cursor elapsed timer
                    _cursor.Elapsed.Start();

                    // call safepoint just before return each document
                    foreach (var doc in pipe.Pipe(nodes, queryPlan))
                    {
                        _cursor.Fetched++;
                        _cursor.Elapsed.Stop();

                        yield return doc;

                        if (transaction.State != TransactionState.Active) throw new LiteException(0, $"There is no more active transaction for this cursor: {_cursor.Query.ToSQL(_cursor.Collection)}");

                        _cursor.Elapsed.Start();
                    }
                }
                finally
                {
                    // stop cursor elapsed
                    _cursor.Elapsed.Stop();

                    transaction.OpenCursors.Remove(_cursor);

                    if (isNew)
                    {
                        _monitor.ReleaseTransaction(transaction);
                    }
                }
            };
        }

        /// <summary>
        /// Execute query and insert result into another collection. Support external collections
        /// </summary>
        internal BsonDataReader ExecuteQueryInto(string into, BsonAutoId autoId)
        {
            IEnumerable<BsonDocument> GetResultset()
            {
                using (var reader = this.ExecuteQuery(false))
                {
                    while (reader.Read())
                    {
                        yield return reader.Current.AsDocument;
                    }
                }
            }

            int result;

            // if collection starts with $ it's system collection
            if (into.StartsWith("$"))
            {
                SqlParser.ParseCollection(new Tokenizer(into), out var name, out var options);

                var sys = _engine.GetSystemCollection(name);

                result = sys.Output(GetResultset(), options);
            }
            // otherwise insert as normal collection
            else
            {
                result = _engine.Insert(into, GetResultset(), autoId);
            }

            return new BsonDataReader(result);
        }
    }
}