using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class LiteEngine : IDisposable
    {
        /// <summary>
        /// Implements insert documents in a collection
        /// </summary>
        public int InsertDocuments(string colName, IEnumerable<BsonDocument> docs)
        {
            lock(_locker)
            try
            {
                // start transaction
                _transaction.Begin();

                var count = 0;
                var col = this.GetCollectionPage(colName, true);

                foreach (var doc in docs)
                {
                    BsonValue id;

                    // add ObjectId to _id if _id not found
                    if (!doc.RawValue.TryGetValue("_id", out id))
                    {
                        id = doc["_id"] = ObjectId.NewObjectId();
                    }

                    // test if _id is a valid type
                    if (id.IsNull || id.IsMinValue || id.IsMaxValue)
                    {
                        _transaction.Rollback();
                        throw LiteException.InvalidDataType("_id", id);
                    }

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

                    count++;
                }

                _transaction.Commit();

                return count;
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
        }
    }
}
