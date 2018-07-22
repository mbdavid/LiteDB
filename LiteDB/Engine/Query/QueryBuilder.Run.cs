using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class to provider a fluent query API to complex queries. This class will be optimied to convert into Query class before run
    /// </summary>
    public partial class QueryBuilder
    {
        /// <summary>
        /// Find for documents in a collection using Query definition
        /// </summary>
        internal BsonDataReader ExecuteQuery(bool explainPlan)
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
                transaction.Dispose();
                throw;
            }

            IEnumerable<BsonValue> RunQuery()
            {
                var snapshot = transaction.CreateSnapshot(_forUpdate ? LockMode.Write : LockMode.Read, _collection, false);
                var cursor = snapshot.NewCursor();

                // if virtual collection, create a virtual collection page
                if (_index is IndexVirtual)
                {
                    snapshot.CollectionPage = IndexVirtual.CreateCollectionPage(_collection);
                }

                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                // no collection, no documents
                if (snapshot.CollectionPage == null)
                {
                    cursor.Timer.Stop();

                    if (isNew)
                    {
                        transaction.Dispose();
                    }
                    yield break;
                }

                // execute optimization before run query (will fill missing _query properties instance)
                var query = this.OptimizeQuery(snapshot);

                // if execution is just to get explan plan, return as single document result
                if (explainPlan)
                {
                    cursor.Timer.Stop();

                    yield return query.GetExplainPlan();

                    if (isNew)
                    {
                        transaction.Dispose();
                    }

                    yield break;
                }

                var loader = query.Index as IDocumentLoader ??
                    (query.IsIndexKeyOnly ?
                        new IndexKeyLoader(indexer, query.Fields.First()) :
                        (IDocumentLoader)new DocumentLoader(data, _engine.Settings.UtcDate, query.Fields));

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = query.Index.Run(snapshot.CollectionPage, indexer)
                        .DistinctBy(x => x.DataBlock, null);

                // get current query pipe: normal or groupby pipe
                using (var pipe = query.GroupBy != null ?
                    new GroupByPipe(_engine, transaction, loader) :
                    (BasePipe)new QueryPipe(_engine, transaction, loader))
                {
                    // commit transaction before close pipe
                    pipe.Disposing += (s, e) =>
                    {
                        if (isNew)
                        {
                            transaction.Commit();
                        }

                        // finish timer and mark cursor as done
                        cursor.Done = true;
                        cursor.Timer.Stop();
                    };

                    // call safepoint just before return each document
                    foreach (var value in pipe.Pipe(nodes, query))
                    {
                        // stop timer and increase counter
                        cursor.Timer.Stop();
                        cursor.FetchCount++;

                        yield return value;

                        // start timer again
                        cursor.Timer.Start();
                    }
                }
            };
        }

        #region Execute Shortcut (First/Single/ToList/...)

        /// <summary>
        /// Find for documents in a collection using Query definition
        /// </summary>
        public BsonDataReader ExecuteReader()
        {
            return this.ExecuteQuery(false);
        }

        /// <summary>
        /// Execute query and return single value 
        /// </summary>
        public BsonValue ExecuteScalar()
        {
            using (var reader = this.ExecuteQuery(false))
            {
                return reader.Current;
            }
        }

        /// <summary>
        /// Execute explain plan over query to check how engine will execute query
        /// </summary>
        public BsonDocument ExecuteExplainPlan()
        {
            using (var reader = this.ExecuteQuery(true))
            {
                return reader.Current.AsDocument;
            }
        }
        /// <summary>
        /// Execute query and return all documents as values
        /// </summary>
        public IEnumerable<BsonValue> ToValues()
        {
            using (var reader = this.ExecuteQuery(false))
            {
                while (reader.Read())
                {
                    yield return reader.Current;
                }
            }
        }

        /// <summary>
        /// Execute query and return documents as IEnumerable
        /// </summary>
        public IEnumerable<BsonDocument> ToEnumerable()
        {
            return this.ToValues().Select(x => x.IsDocument ? x.AsDocument : new BsonDocument { ["expr"] = x });
        }

        /// <summary>
        /// Execute query and return as List of BsonDocument
        /// </summary>
        public List<BsonDocument> ToList()
        {
            return this.ToEnumerable().ToList();
        }

        /// <summary>
        /// Execute query and return as array of BsonDocument
        /// </summary>
        public BsonDocument[] ToArray()
        {
            return this.ToEnumerable().ToArray();
        }

        /// <summary>
        /// Execute Single over ToEnumerable result documents
        /// </summary>
        public BsonDocument SingleById(BsonValue id)
        {
            return this
                .Index(LiteDB.Engine.Index.EQ("_id", id))
                .Single();
        }

        /// <summary>
        /// Execute Single over ToEnumerable result documents
        /// </summary>
        public BsonDocument Single()
        {
            return this.ToEnumerable().Single();
        }

        /// <summary>
        /// Execute SingleOrDefault over ToEnumerable result documents
        /// </summary>
        public BsonDocument SingleOrDefault()
        {
            return this.ToEnumerable().SingleOrDefault();
        }

        /// <summary>
        /// Execute First over ToEnumerable result documents
        /// </summary>
        public BsonDocument First()
        {
            return this.ToEnumerable().First();
        }

        /// <summary>
        /// Execute FirstOrDefault over ToEnumerable result documents
        /// </summary>
        public BsonDocument FirstOrDefault()
        {
            return this.ToEnumerable().FirstOrDefault();
        }

        /// <summary>
        /// Execute count over document resultset
        /// </summary>
        public int Count()
        {
            return this.ToValues().Count();
        }

        /// <summary>
        /// Execute count over document resultset
        /// </summary>
        public long LongCount()
        {
            return this.ToValues().LongCount();
        }

        /// <summary>
        /// Execute exists over document resultset
        /// </summary>
        public bool Exists()
        {
            return this.ToValues().Any();
        }

        /// <summary>
        /// Execute query and insert result into new collection
        /// </summary>
        public int Into(string newCollection, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            return _engine.Insert(newCollection, this.ToEnumerable(), autoId);
        }

        /// <summary>
        /// Execute query and save data into FileCollection implementation
        /// </summary>
        public int Into(IFileCollection collection)
        {
            return collection.Output(this.ToValues());
        }

        #endregion
    }
}