using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to pipe documents and apply Load/Filter/Includes/OrderBy commands
    /// </summary>
    internal class QueryPipe
    {
        private readonly LiteEngine _engine;
        private readonly LiteTransaction _transaction;
        private readonly IDocumentLoader _loader;

        public QueryPipe(LiteEngine engine, LiteTransaction transaction, IDocumentLoader loader)
        {
            _engine = engine;
            _transaction = transaction;
            _loader = loader;
        }

        /// <summary>
        /// Start pipe documents process
        /// </summary>
        public IEnumerable<BsonDocument> Pipe(IEnumerable<BsonDocument> source, Query query)
        {
            // do includes in result before filter
            foreach (var path in query.IncludeBefore)
            {
                source = this.Include(source, path);
            }

            // filter results according expressions
            foreach (var expr in query.Filters)
            {
                source = this.Filter(source, expr);
            }

            if (query.OrderBy != null)
            {
                // pipe: orderby with offset+limit
                source = this.OrderBy(source, query.OrderBy, query.Order, query.Offset, query.Limit);
            }
            else
            {
                // pipe: apply offset (no orderby)
                if (query.Offset > 0) source = source.Skip(query.Offset);

                // pipe: apply limit (no orderby)
                if (query.Limit < int.MaxValue) source = source.Take(query.Limit);
            }

            // do includes in result before filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // transfom result if contains select expression
            if (query.Select != null)
            {
                source = this.Select(source, query.Select);
            }

            // return document pipe
            return source;
        }

        /// <summary>
        /// Pipe: Do include in result document according path expression
        /// </summary>
        private IEnumerable<BsonDocument> Include(IEnumerable<BsonDocument> source, BsonExpression path)
        {
            foreach(var doc in source)
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

                    // create query for find by _id
                    var query = new Query
                    {
                        Index = Index.EQ("_id", refId)
                    };

                    //TODO implement include again
                    // now, find document reference
                    // var refDoc = _engine.Find(refCol, query, _transaction).FirstOrDefault();
                    // 
                    // // if found, change with current document
                    // if (refDoc != null)
                    // {
                    //     value.Remove("$id");
                    //     value.Remove("$ref");
                    // 
                    //     refDoc.CopyTo(value);
                    // }
                    // else
                    // {
                    //     // remove value from parent (document or array)
                    //     value.Destroy();
                    // }
                }

                yield return doc;
            }
        }

        /// <summary>
        /// Pipe: Filter document according expression. Expression must be an Bool result
        /// </summary>
        private IEnumerable<BsonDocument> Filter(IEnumerable<BsonDocument> source, BsonExpression expr)
        {
            foreach(var doc in source)
            {
                var result = expr.Execute(doc, true).FirstOrDefault();

                // expression must return an boolean and be true to return document
                if (result.IsBoolean && result.AsBoolean == true)
                {
                    yield return doc;
                }
            }
        }

        /// <summary>
        /// Pipe: Transaform final result appling expressin transform. Expression must return an BsonDocument (or will be converter into a new documnet)
        /// </summary>
        private IEnumerable<BsonDocument> Select(IEnumerable<BsonDocument> source, BsonExpression expr)
        {
            foreach(var doc in source)
            {
                var result = expr.Execute(doc, true);

                var value = result.First();

                if (value.IsDocument)
                {
                    yield return value.AsDocument;
                }
                else
                {
                    yield return new BsonDocument { ["expr"] = value };
                }
            }
        }

        /// <summary>
        /// Pipe: OrderBy documents according orderby expression/order
        /// </summary>
        private IEnumerable<BsonDocument> OrderBy(IEnumerable<BsonDocument> source, BsonExpression expr, int order, int offset, int limit)
        {
            IEnumerable<BsonDocument> DoOrderBy(LiteTransaction transaction, Snapshot snapshot)
            {
                var indexer = new IndexService(snapshot);

                // create new page as collection page (with no CollectionListPage reference)
                var col = snapshot.NewPage<CollectionPage>();
                var index = indexer.CreateIndex(col);

                // get head/tail index node
                var head = indexer.GetNode(index.HeadNode);
                var tail = indexer.GetNode(index.TailNode);

                var last = order == Query.Ascending ? BsonValue.MaxValue : BsonValue.MinValue;
                var total = limit == int.MaxValue ? int.MaxValue : offset + limit;
                var indexCounter = 0;

                foreach (var doc in source)
                {
                    // get key to be sorted
                    var key = expr.Execute(doc, true).First();
                    var diff = key.CompareTo(last);

                    // add to list only if lower than last space
                    if ((order == Query.Ascending && diff < 1) ||
                        (order == Query.Descending && diff > -1))
                    {
                        var tmpNode = indexer.AddNode(index, key, null);

                        // use rawId (position of document inside datafile)
                        tmpNode.DataBlock = doc.RawId;
                        tmpNode.CacheDocument = doc;

                        indexCounter++;

                        // exceeded limit
                        if (indexCounter > total)
                        {
                            var exceeded = (order == Query.Ascending) ? tail.Prev[0] : head.Next[0];

                            indexer.Delete(index, exceeded);

                            var lnode = (order == Query.Ascending) ? tail.Prev[0] : head.Next[0];

                            last = indexer.GetNode(lnode).Key;

                            indexCounter--;
                        }

                        // if memory pages excedded limit size, flush to temp disk
                        // transaction.Safepoint();
                    }
                }

                var find = indexer.FindAll(index, order).Skip(offset).Take(limit);

                foreach (var node in find)
                {
                    // if document are in cache, use it. if not, get from disk again
                    var doc = node.CacheDocument;

                    if (doc == null)
                    {
                        doc = _loader.Load(node.DataBlock);
                    }

                    yield return doc;
                }
            }

            // using tempdb for store sort data
            using (var transaction = _engine.TempDB.BeginTrans())
            {
                // open read transaction because i dont want save in disk (only in wal if exceed memory usage)
                return transaction.CreateSnapshot(SnapshotMode.Read, Guid.NewGuid().ToString("n"), false, snapshot =>
                {
                    return DoOrderBy(transaction, snapshot);
                });
            }
        }
    }
}