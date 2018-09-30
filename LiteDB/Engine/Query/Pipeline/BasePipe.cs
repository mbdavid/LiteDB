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
        protected readonly CursorInfo _cursor;

        public BasePipe(LiteEngine engine, TransactionService transaction, IDocumentLoader loader, CursorInfo cursor)
        {
            _engine = engine;
            _transaction = transaction;
            _loader = loader;
            _cursor = cursor;
        }

        /// <summary>
        /// Abstract method to be implement according pipe workflow
        /// </summary>
        public abstract IEnumerable<BsonDocument> Pipe(IEnumerable<IndexNode> nodes, QueryPlan query);

        // load documents from disk or make a "fake" document using index key only (useful for COUNT/EXISTS)
        protected IEnumerable<BsonDocument> LoadDocument(IEnumerable<IndexNode> nodes, bool indexKeyOnly, string field)
        {
            DEBUG(indexKeyOnly && field == null, "should not be indexOnly = null with no field name");

            foreach (var node in nodes)
            {
                // check if transaction all full of pages to clear before continue
                _transaction.Safepoint();

                // if is indexKeyOnly, load here from IndexNode, otherwise, read from Loader

                yield return indexKeyOnly ?
                    new BsonDocument { [field] = node.Key, RawId = node.Position } :
                    _loader.Load(node.DataBlock);
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
                foreach (var value in path.Execute(doc, false)
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

                        //TODO: oferecer suporte a include com campos selecionados!!
                        loader = new DocumentLoader(data, _engine.UtcDate, null, _cursor);

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
                            var refDoc = loader.Load(node.DataBlock);

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
                var result = expr.Execute(doc, true)
                    .Where(x => x.IsBoolean && x.AsBoolean == true)
                    .Any();

                // expression must return an boolean and be true to return document
                if (result)
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