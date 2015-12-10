using System;
using System.Collections.Generic;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Implements insert documents in a collection - use a buffer to commit transaction in each buffer count
        /// </summary>
        public int Insert(string colName, IEnumerable<BsonDocument> docs)
        {
            return this.Transaction<int>(colName, true, (col) =>
            {
                var count = 0;

                foreach (var doc in docs)
                {
                    BsonValue id;

                    // if no _id, add one as ObjectId
                    if (!doc.RawValue.TryGetValue("_id", out id))
                    {
                        doc["_id"] = id = ObjectId.NewObjectId();
                    }

                    // test if _id is a valid type
                    if (id.IsNull || id.IsMinValue || id.IsMaxValue)
                    {
                        throw LiteException.InvalidDataType("_id", id);
                    }

                    _log.Write(Logger.COMMAND, "insert document on '{0}' :: _id = {1}", col.CollectionName, id);

                    // serialize object
                    var bytes = BsonSerializer.Serialize(doc);

                    // storage in data pages - returns dataBlock address
                    var dataBlock = _data.Insert(col, bytes);

                    // store id in a PK index [0 array]
                    var pk = _indexer.AddNode(col.PK, id);

                    // do links between index <-> data block
                    pk.DataBlock = dataBlock.Position;
                    dataBlock.IndexRef[0] = pk.Position;

                    // for each index, insert new IndexNode
                    foreach (var index in col.GetIndexes(false))
                    {
                        var key = doc.Get(index.Field);

                        var node = _indexer.AddNode(index, key);

                        // point my index to data object
                        node.DataBlock = dataBlock.Position;

                        // point my dataBlock
                        dataBlock.IndexRef[index.Slot] = node.Position;
                    }

                    _cache.CheckPoint();

                    count++;
                }

                return count;
            });
        }
    }
}