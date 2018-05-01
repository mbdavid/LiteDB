using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to provider a fluent query API to complex queries. This class will be optimied to convert into Query class before run
    /// </summary>
    public partial class QueryBuilder
    {
        /// <summary>
        /// Find for documents in a collection using Query definition
        /// </summary>
        internal IEnumerable<BsonValue> Run()
        {
            var transaction = _engine.GetTransaction(out var isNew);

            try
            {
                // encapsulate all execution to catch any error
                return RunQuery();
            }
            catch
            {
                // if any error, rollback transaction
                transaction.Dispose();
                throw;
            }

            IEnumerable<BsonValue> RunQuery()
            {
                var snapshot = transaction.CreateSnapshot(_query.ForUpdate ? SnapshotMode.Write : SnapshotMode.Read, _collection, false);

                // if virtual collection, create a virtual collection page
                if (_query.IsVirtual)
                {
                    snapshot.CollectionPage = IndexVirtual.CreateCollectionPage(_collection);
                }

                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                // no collection, no documents
                if (col == null) yield break;

                // execute optimization before run query (will fill missing _query properties instance)
                this.OptimizeQuery(snapshot);

                // load only query fields (null return all document)
                var fields = _query.Select?.Fields;

                if (fields != null)
                {
                    // if partial document load, add filter, groupby, orderby fields too
                    fields.AddRange(_query.Filters.SelectMany(x => x.Fields));
                    fields.AddRange(_query.GroupBy?.Fields);
                    fields.AddRange(_query.OrderBy?.Fields);
                }

                var loader = _query.IsVirtual ?
                    (IDocumentLoader)_query.Index :
                    new DocumentLoader(data, _engine.UtcDate, fields);

                //TODO: remove this execution plan
                Console.WriteLine(_query.GetExplainPlan());

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = _query.Index.Run(col, indexer)
                        .DistinctBy(x => x.DataBlock, null);

                // get current query pipe: normal or groupby pipe
                var pipe = _query.GroupBy != null ?
                    new GroupByPipe(_engine, transaction, loader) :
                    (BasePipe)new QueryPipe(_engine, transaction, loader);

                // call safepoint just before return each document
                foreach (var value in pipe.Pipe(nodes, _query))
                {
                    transaction.Safepoint();

                    yield return value;
                }

                // if is a new transaction, dipose now
                if (isNew)
                {
                    transaction.Dispose();
                }
            };
        }

        #region Execute Shortcut (First/Single/ToList/...)

        /// <summary>
        /// Execute query and return documents as IEnumerable
        /// </summary>
        public IEnumerable<BsonDocument> ToEnumerable()
        {
            return this.Run().Select(x => x as BsonDocument);
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
                .Index(LiteDB.Index.EQ("_id", id))
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

            return this.Run().FirstOrDefault();
        }

        /// <summary>
        /// Execute count over document resultset
        /// </summary>
        public int Count()
        {
            return this.Run().Count();
        }

        /// <summary>
        /// Execute exists over document resultset
        /// </summary>
        public bool Exists()
        {
            return this.Run().Any();
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