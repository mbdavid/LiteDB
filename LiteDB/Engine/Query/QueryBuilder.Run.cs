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
        internal IEnumerable<BsonDocument> Run(bool countOnly)
        {
            // support _transaction be null - will create new transaction for single execution
            var newTransaction = _transaction == null;

            try
            {
                // create new transaction if null
                if (newTransaction)
                {
                    _transaction = _engine.BeginTrans();
                }

                // call DoFind inside snapshot
                return _transaction.CreateSnapshot(_query.ForUpdate ? SnapshotMode.Write : SnapshotMode.Read, _collection, false, snapshot =>
                {
                    // execute optimization before run query (will fill missing _query properties instance)
                    this.OptimizeQuery(snapshot);

                    //TODO: remove this execution plan
                    Console.WriteLine(_query.GetExplainPlan());

                    return DoFind(snapshot);
                });

            }
            catch
            {
                // if throw any error, dispose new transaction before throw
                if (newTransaction && _transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                throw;
            }

            // executing query
            IEnumerable<BsonDocument> DoFind(Snapshot snapshot)
            {
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);
                var loader = new DocumentLoader(data, _engine.BsonReader);

                // no collection, no documents
                if (col == null) yield break;

                // get node list from query - distinct by dataBlock (avoid duplicate) and skip
                var nodes = _query.Index.Run(col, indexer)
                    .DistinctBy(x => x.DataBlock, null)
                    .Skip(_query.Offset);

                // load document from disk
                var docs = LoadDocument(nodes, loader, _query.KeyOnly, _query.Index.Name);

                // load pipe query to apply all query options
                var pipe = new QueryPipe(_engine, _transaction, loader);

                // call safepoint just before return each document
                foreach (var doc in pipe.Pipe(docs, _query))
                {
                    _transaction.Safepoint();

                    yield return doc;
                }

                // dipose transaction after read all resultset (if is new transaction)
                if (newTransaction && _transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }

            // load documents from disk or make a "fake" document using key only (useful for COUNT/EXISTS)
            IEnumerable<BsonDocument> LoadDocument(IEnumerable<IndexNode> nodes, IDocumentLoader loader, bool keyOnly, string name)
            {
                foreach (var node in nodes)
                {
                    yield return keyOnly ?
                        new BsonDocument { [name] = node.Key } :
                        loader.Load(node.DataBlock);
                }
            }
        }

        #region Execute Shortcut (First/Single/ToList/...)

        /// <summary>
        /// Execute query and return documents as IEnumerable
        /// </summary>
        public List<BsonDocument> ToEnumerable()
        {
            return this.Run(false).ToList();
        }

        /// <summary>
        /// Execute query and return as List of BsonDocument
        /// </summary>
        public List<BsonDocument> ToList()
        {
            return this.Run(false).ToList();
        }

        /// <summary>
        /// Execute query and return as array of BsonDocument
        /// </summary>
        public BsonDocument[] ToArray()
        {
            return this.Run(false).ToArray();
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
            return this.Run(false).Single();
        }

        /// <summary>
        /// Execute SingleOrDefault over ToEnumerable result documents
        /// </summary>
        public BsonDocument SingleOrDefault()
        {
            return this.Run(false).SingleOrDefault();
        }

        /// <summary>
        /// Execute First over ToEnumerable result documents
        /// </summary>
        public BsonDocument First()
        {
            return this.Run(false).First();
        }

        /// <summary>
        /// Execute FirstOrDefault over ToEnumerable result documents
        /// </summary>
        public BsonDocument FirstOrDefault()
        {
            return this.Run(false).FirstOrDefault();
        }

        /// <summary>
        /// Execute count over document resultset
        /// </summary>
        public int Count()
        {
            return this.Run(true).Count();
        }

        /// <summary>
        /// Execute exists over document resultset
        /// </summary>
        public bool Exists()
        {
            return this.Run(true).Any();
        }

        #endregion
    }
}