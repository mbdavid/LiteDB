using System;
using System.Collections.Generic;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Implement update command to a document inside a collection
        /// </summary>
        public int Update(string colName, IEnumerable<BsonDocument> docs)
        {
            return this.Transaction<int>(colName, false, (col) =>
            {
                // no collection, no updates
                if (col == null) return 0;

                var count = 0;

                foreach (var doc in docs)
                {
                    // normalize id before find
                    var id = doc["_id"].Normalize(col.PK.Options);

                    // validate id for null, min/max values
                    if (id.IsNull || id.IsMinValue || id.IsMaxValue)
                    {
                        throw LiteException.InvalidDataType("_id", id);
                    }

                    _log.Write(Logger.COMMAND, "update document on '{0}' :: _id = ", colName, id);

                    // find indexNode from pk index
                    var indexNode = _indexer.Find(col.PK, id, false, Query.Ascending);

                    // if not found document, no updates
                    if (indexNode == null) continue;

                    // serialize document in bytes
                    var bytes = BsonSerializer.Serialize(doc);

                    // update data storage
                    var dataBlock = _data.Update(col, indexNode.DataBlock, bytes);

                    // delete/insert indexes - do not touch on PK
                    foreach (var index in col.GetIndexes(false))
                    {
                        var key = doc.Get(index.Field);

                        var node = _indexer.GetNode(dataBlock.IndexRef[index.Slot]);

                        // check if my index node was changed
                        if (node.Key.CompareTo(key) != 0)
                        {
                            // remove old index node
                            _indexer.Delete(index, node.Position);

                            // and add a new one
                            var newNode = _indexer.AddNode(index, key);

                            // point my index to data object
                            newNode.DataBlock = dataBlock.Position;

                            // set my block page as dirty before change
                            _pager.SetDirty(dataBlock.Page);

                            // point my dataBlock
                            dataBlock.IndexRef[index.Slot] = newNode.Position;
                        }
                    }

                    _cache.CheckPoint();

                    count++;
                }

                return count;
            });
        }
    }
}