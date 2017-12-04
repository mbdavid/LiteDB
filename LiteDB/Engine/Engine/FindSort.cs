using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Max dirty pages used in memory for sort operation. After this limit, persist all pages into disk, clear memory pages, and continue sorting
        /// </summary>
        private const int MAX_SORT_PAGES = 5000; // ~ 20Mb?

        /// <summary>
        /// EXPERIMENTAL Find with sort operation - use memory or disk (temp file) to sort
        /// </summary>
        public List<BsonDocument> FindSort(string collection, Query query, string orderBy, int order = Query.Ascending, int skip = 0, int limit = int.MaxValue)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            _log.Write(Logger.COMMAND, "query-sort documents in '{0}' => {1}", collection, query);

            // evaluate orderBy path/expression
            var expr = new BsonExpression(orderBy);

            // lock database for read access
            using (_locker.Read())
            {
                var last = order == Query.Ascending ? BsonValue.MaxValue : BsonValue.MinValue;
                var total = limit == int.MaxValue ? int.MaxValue : skip + limit;
                var indexCounter = 0;
                var disk = new TempDiskService();

                // create memory database
                using (var engine = new LiteEngine(disk))
                {
                    // get collection page
                    var col = this.GetCollectionPage(collection, false);

                    if (col == null) return new List<BsonDocument>();

                    // create a temp collection in new memory database
                    var tmp = engine._collections.Add("tmp");

                    // create index pointer
                    var index = engine._indexer.CreateIndex(tmp);

                    // get head/tail index node
                    var head = engine._indexer.GetNode(index.HeadNode);
                    var tail = engine._indexer.GetNode(index.TailNode);

                    // first lets works only with index in query
                    var nodes = query.Run(col, _indexer);

                    foreach (var node in nodes)
                    {
                        var buffer = _data.Read(node.DataBlock);
                        var doc = _bsonReader.Deserialize(buffer).AsDocument;

                        // if needs use filter
                        if (query.UseFilter && query.FilterDocument(doc) == false) continue;

                        // get key to be sorted
                        var key = expr.Execute(doc, true).First();
                        var diff = key.CompareTo(last);

                        // add to list only if lower than last space
                        if ((order == Query.Ascending && diff < 1) || 
                            (order == Query.Descending && diff > -1))
                        {
                            var tmpNode = engine._indexer.AddNode(index, key, null);

                            tmpNode.DataBlock = node.DataBlock;
                            tmpNode.CacheDocument = doc;

                            indexCounter++;

                            // exceeded limit
                            if (indexCounter > total)
                            {
                                var exceeded = (order == Query.Ascending) ? tail.Prev[0] : head.Next[0];

                                engine._indexer.Delete(index, exceeded);

                                var lnode = (order == Query.Ascending) ? tail.Prev[0] : head.Next[0];

                                last = engine._indexer.GetNode(lnode).Key;

                                indexCounter--;
                            }

                            // if memory pages excedded limit size, flush to disk
                            if (engine._cache.DirtyUsed > MAX_SORT_PAGES)
                            {
                                engine._trans.PersistDirtyPages();

                                engine._trans.CheckPoint();
                            }
                        }
                    }

                    var result = new List<BsonDocument>();

                    // if skip is lower than limit, take nodes from skip from begin
                    // if skip is higher than limit, take nodes from end and revert order (avoid lots of skip)
                    var find = skip < limit ?
                        engine._indexer.FindAll(index, order).Skip(skip).Take(limit) : // get from original order
                        engine._indexer.FindAll(index, -order).Take(limit).Reverse(); // avoid long skips, take from end and revert

                    // --- foreach (var node in engine._indexer.FindAll(index, order).Skip(skip).Take(limit))
                    foreach (var node in find)
                    {
                        // if document are in cache, use it. if not, get from disk again
                        var doc = node.CacheDocument;

                        if (doc == null)
                        {
                            var buffer = _data.Read(node.DataBlock);
                            doc = _bsonReader.Deserialize(buffer).AsDocument;
                        }

                        result.Add(doc);
                    }

                    return result;
                }
            }
        }
    }
}