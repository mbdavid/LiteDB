using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Abstract class with workflow method to be used in pipeline implementation
    /// </summary>
    internal abstract class BasePipe : IDisposable
    {
        public event EventHandler Disposing = null;

        protected readonly LiteEngine _engine;
        protected readonly TransactionService _transaction;
        protected readonly IDocumentLoader _loader;

        public BasePipe(LiteEngine engine, TransactionService transaction, IDocumentLoader loader)
        {
            _engine = engine;
            _transaction = transaction;
            _loader = loader;
        }

        /// <summary>
        /// Abstract method to be implement according pipe workflow
        /// </summary>
        public abstract IEnumerable<BsonDocument> Pipe(IEnumerable<IndexNode> nodes, QueryPlan query);

        // load documents from document loader
        protected IEnumerable<BsonDocument> LoadDocument(IEnumerable<IndexNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return _loader.Load(node);

                // check if transaction all full of pages to clear before continue
                _transaction.Safepoint();
            }
        }

        /// <summary>
        /// Pipe: Do include in result document according path expression
        /// </summary>
        protected IEnumerable<BsonDocument> Include(IEnumerable<BsonDocument> source, BsonExpression path)
        {
            // cached services
            string last = null;
            Snapshot snapshot = null;
            IndexService indexer = null;
            DataService data = null;
            CollectionIndex index = null;
            IDocumentLoader loader = null;

            foreach (var doc in source)
            {
                foreach (var value in path.Execute(doc)
                                        .Where(x => x.IsDocument)
                                        .Select(x => x.AsDocument)
                                        .ToList())
                {
                    // works only if is a document
                    var refId = value["$id"];
                    var refCol = value["$ref"];

                    // if has no reference, just go out
                    if (refId.IsNull || !refCol.IsString) continue;

                    // do some cache re-using when is same $ref (almost always is the same $ref collection)
                    if (last != refCol.AsString)
                    {
                        last = refCol.AsString;

                        // initialize services
                        snapshot = _transaction.CreateSnapshot(LockMode.Read, last, false);
                        indexer = new IndexService(snapshot);
                        data = new DataService(snapshot);

                        loader = new DocumentLoader(data, _engine.Settings.UtcDate, null);

                        index = snapshot.CollectionPage?.PK;
                    }

                    // if there is no ref collection
                    if (index == null)
                    {
                        value.Destroy();
                    }
                    else
                    {
                        var node = indexer.Find(index, refId, false, Query.Ascending);

                        // if _id was not found in $ref collection, remove value
                        if (node == null)
                        {
                            value.Destroy();
                        }
                        else
                        {
                            // load document based on dataBlock position
                            var refDoc = loader.Load(node);

                            value.Remove("$id");
                            value.Remove("$ref");
                            
                            refDoc.CopyTo(value);
                        }
                    }
                }

                yield return doc;
            }
        }

        /// <summary>
        /// Pipe: Filter document according expression. Expression must be an Bool result
        /// </summary>
        protected IEnumerable<BsonDocument> Filter(IEnumerable<BsonDocument> source, BsonExpression expr)
        {
            foreach(var doc in source)
            {
                // checks if any result of expression is true
                var result = expr.ExecuteScalar(doc);

                if(result.IsBoolean && result.AsBoolean)
                {
                    yield return doc;
                }
            }
        }

        /// <summary>
        /// Pipe: OrderBy documents according orderby expression/order
        /// </summary>
        protected IEnumerable<BsonDocument> OrderBy(IEnumerable<BsonDocument> source, BsonExpression expr, int order, int offset, int limit)
        {
            //TODO: temp in-memory orderby implementation
            var query = source
                .Select(x => new { order = expr.Execute(x).First(), doc = x });

            if (order == Query.Ascending)
            {
                query = query.OrderBy(x => x.order);
            }
            else if(order == Query.Descending)
            {
                query = query.OrderByDescending(x => x.order);
            }

            return query
                .Select(x => x.doc)
                .Skip(offset)
                .Take(limit);
        }

        public void Dispose()
        {
            // call disposing event
            this.Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}