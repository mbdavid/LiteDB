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
            // call DoFind inside snapshot
            var snapshot = _transaction.CreateSnapshot(_query.ForUpdate ? SnapshotMode.Write : SnapshotMode.Read, _collection, false);

            var col = snapshot.CollectionPage;
            var data = new DataService(snapshot);
            var indexer = new IndexService(snapshot);
            var loader = new DocumentLoader(data, _engine.BsonReader);

            // no collection, no documents
            if (col == null) yield break;

            // execute optimization before run query (will fill missing _query properties instance)
            this.OptimizeQuery(snapshot);

            //TODO: remove this execution plan
            Console.WriteLine(_query.GetExplainPlan());

            // get node list from query - distinct by dataBlock (avoid duplicate)
            var nodes = _query.Index.Run(col, indexer)
                    .DistinctBy(x => x.DataBlock, null);

            // get corrent query pipe: normal or groupby pipe
            var pipe = _query.GroupBy != null ?
                new GroupByPipe(_engine, _transaction, loader) :
                (BasePipe)new QueryPipe(_engine, _transaction, loader);

            // call safepoint just before return each document
            foreach (var value in pipe.Pipe(nodes, _query))
            {
                _transaction.Safepoint();

                yield return value;
            }
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
            return _engine.Insert(newCollection, this.ToEnumerable(), autoId, _transaction);
        }

        #endregion
    }
}