using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class that execute QueryPlan returing results
    /// </summary>
    internal class QueryExecutor
    {
        private readonly LiteEngine _engine;
        private readonly string _collection;
        private readonly QueryDefinition _queryDefinition;
        private readonly IEnumerable<BsonDocument> _source;

        public QueryExecutor(LiteEngine engine, string collection, QueryDefinition query, IEnumerable<BsonDocument> source)
        {
            _engine = engine;
            _collection = collection;
            _queryDefinition = query;

            // source will be != null when query will run over external data source, like system collections or files (not user collection)
            _source = source;
        }

        public BsonDataReader ExecuteQuery()
        {
            if (_queryDefinition.Into == null)
            {
                return this.ExecuteQuery(_queryDefinition.ExplainPlan);
            }
            else
            {
                return this.ExecuteQueryInto(_queryDefinition.Into, _queryDefinition.IntoAutoId);
            }
        }

        /// <summary>
        /// Run query definition into engine. Execute optimization to get query planner
        /// </summary>
        internal BsonDataReader ExecuteQuery(bool executionPlan)
        {
            var transaction = _engine.GetTransaction(true, out var isNew);

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
                var snapshot = transaction.CreateSnapshot(_queryDefinition.ForUpdate ? LockMode.Write : LockMode.Read, _collection, false);
                var cursor = _engine.NewCursor(transaction, snapshot);

                // if query will run over external data source, create fake collection page from virtual index
                if (_source != null)
                {
                    snapshot.CollectionPage = IndexVirtual.CreateCollectionPage(_collection);
                }

                cursor.Start();

                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                // no collection, no documents
                if (snapshot.CollectionPage == null)
                {
                    cursor.Finish();

                    if (isNew)
                    {
                        transaction.Release();
                    }
                    yield break;
                }

                // execute optimization before run query (will fill missing _query properties instance)
                var optimizer = new QueryOptimization(snapshot, _queryDefinition, _source);

                // check if query definition rules are ok
                optimizer.Validate();

                var queryPlan = optimizer.ProcessQuery();

                // if execution is just to get explan plan, return as single document result
                if (executionPlan)
                {
                    cursor.Timer.Stop();

                    yield return queryPlan.GetExecutionPlan();

                    if (isNew)
                    {
                        transaction.Release();
                    }

                    yield break;
                }

                // define document loader
                if (!(queryPlan.Index is IDocumentLoader loader)) // use index as document loader (virtual collection)
                {
                    if (queryPlan.IsIndexKeyOnly)
                    {
                        loader = new IndexKeyLoader(indexer, queryPlan.Fields.Single());
                    }

                    loader = new CachedDocumentLoader(data, _engine.UtcDate, queryPlan.Fields, cursor, 1000);
                    //loader = new DocumentLoader(data, _engine.UtcDate, queryPlan.Fields, cursor);
                }

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = queryPlan.Index.Run(snapshot.CollectionPage, indexer)
                        .DistinctBy(x => x.DataBlock, null);

                // get current query pipe: normal or groupby pipe
                using (var pipe = queryPlan.GroupBy != null ?
                    new GroupByPipe(_engine, transaction, loader, cursor) :
                    (BasePipe)new QueryPipe(_engine, transaction, loader, cursor))
                {
                    // commit transaction before close pipe
                    pipe.Disposing += (s, e) =>
                    {
                        if (isNew)
                        {
                            //TODO: this was .Commit() before, but I don't remember why... if is a new transaction, just release
                            transaction.Release();
                        }

                        // finish timer and mark cursor as done
                        cursor.Done = true;
                        cursor.Timer.Stop();
                    };

                    // call safepoint just before return each document
                    foreach (var doc in pipe.Pipe(nodes, queryPlan))
                    {
                        // stop timer and increase counter
                        cursor.Timer.Stop();
                        cursor.DocumentCount++;

                        yield return doc;

                        // start timer again
                        cursor.Timer.Start();
                    }
                }
            };
        }

        /// <summary>
        /// Execute query and insert result into another collection. Support external collections
        /// </summary>
        internal BsonDataReader ExecuteQueryInto(string into, BsonAutoId autoId)
        {
            IEnumerable<BsonValue> getResultset()
            {
                using (var reader = this.ExecuteQuery(false))
                {
                    while(reader.Read())
                    {
                        yield return reader.Current;
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
                result = _engine.Insert(into, getResultset().Select(x => x.AsDocument), autoId);
            }

            return new BsonDataReader(result);
        }
    }
}