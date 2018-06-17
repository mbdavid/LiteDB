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
        public BsonDataReader ExecuteReader()
        {
            var transaction = _engine.GetTransaction(true, out var isNew);

            try
            {
                // encapsulate all execution to catch any error
                return new BsonDataReader(RunQuery(), _query);
            }
            catch
            {
                // if any error, rollback transaction
                transaction.Dispose();
                throw;
            }

            IEnumerable<BsonValue> RunQuery()
            {
                var snapshot = transaction.CreateSnapshot(_query.ForUpdate ? SnapshotMode.Write : SnapshotMode.Read, _query.Collection, false);

                // if virtual collection, create a virtual collection page
                if (_query.IsVirtual)
                {
                    snapshot.CollectionPage = IndexVirtual.CreateCollectionPage(_query.Collection);
                }

                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                // no collection, no documents
                if (snapshot.CollectionPage == null)
                {
                    if (isNew)
                    {
                        transaction.Dispose();
                    }
                    yield break;
                }

                // execute optimization before run query (will fill missing _query properties instance)
                this.OptimizeQuery(snapshot);

                var loader = _query.IsVirtual ?
                    (IDocumentLoader)_query.Index :
                    new DocumentLoader(data, _engine.Settings.UtcDate, _query.Fields);

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = _query.Index.Run(snapshot.CollectionPage, indexer)
                        .DistinctBy(x => x.DataBlock, null);

                // get current query pipe: normal or groupby pipe
                using (var pipe = _query.GroupBy != null ?
                    new GroupByPipe(_engine, transaction, isNew, loader) :
                    (BasePipe)new QueryPipe(_engine, transaction, isNew, loader))
                {
                    // call safepoint just before return each document
                    foreach (var value in pipe.Pipe(nodes, _query))
                    {
                        transaction.Safepoint();

                        yield return value;
                    }
                }
            };
        }

        #region Execute Shortcut (First/Single/ToList/...)

        /// <summary>
        /// Execute query and return all documents as values
        /// </summary>
        public IEnumerable<BsonValue> ToValues()
        {
            using (var reader = this.ExecuteReader())
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
        /// Execute query running SELECT expression over all resultset
        /// </summary>
        public BsonValue Aggregate()
        {
            _query.Aggregate = true;

            return this.ExecuteReader().Current;
        }

        /// <summary>
        /// Execute count over document resultset
        /// </summary>
        public int Count()
        {
            return this.ToEnumerable().Count();
        }

        /// <summary>
        /// Execute exists over document resultset
        /// </summary>
        public bool Exists()
        {
            return this.ExecuteReader().HasValues;
        }

        /// <summary>
        /// Execute query and insert result into new collection
        /// </summary>
        public int Into(string newCollection, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            return _engine.Insert(newCollection, this.ToEnumerable(), autoId);
        }

        #endregion
    }
}