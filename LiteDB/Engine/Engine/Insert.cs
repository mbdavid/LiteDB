using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implements insert documents in a collection - use a buffer to commit transaction in each buffer count
        /// </summary>
        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            return transaction.CreateSnapshot(SnapshotMode.Write, collection, true, snapshot =>
            {
                var col = snapshot.CollectionPage;
                var count = 0;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                foreach (var doc in docs)
                {
                    this.InsertDocument(snapshot, col, doc, autoId, indexer, data);

                    transaction.Safepoint();

                    count++;
                }

                return count;
            });
        }

        /// <summary>
        /// Internal implementation of insert a document
        /// </summary>
        private void InsertDocument(Snapshot snapshot, CollectionPage col, BsonDocument doc, BsonAutoId autoId, IndexService indexer, DataService data)
        {
            // increase collection sequence _id
            col.Sequence++;

            snapshot.SetDirty(col);

            // if no _id, add one
            if (!doc.RawValue.TryGetValue("_id", out var id))
            {
                doc["_id"] = id =
                    autoId == BsonAutoId.ObjectId ? new BsonValue(ObjectId.NewObjectId()) :
                    autoId == BsonAutoId.Guid ? new BsonValue(Guid.NewGuid()) :
                    autoId == BsonAutoId.DateTime ? new BsonValue(DateTime.Now) :
                    autoId == BsonAutoId.Int32 ? new BsonValue((Int32)col.Sequence) :
                    autoId == BsonAutoId.Int64 ? new BsonValue(col.Sequence) : BsonValue.Null;
            }
            // create bubble in sequence number if _id is bigger than current sequence
            else if(autoId == BsonAutoId.Int32 || autoId == BsonAutoId.Int64)
            {
                var current = id.AsInt64;

                // if current id is bigger than sequence, jump sequence to this number. Other was, do not increse sequnce
                col.Sequence = current >= col.Sequence ? current : col.Sequence - 1;
            }

            // test if _id is a valid type
            if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            {
                throw LiteException.InvalidDataType("_id", id);
            }

            // serialize object
            var bytes = _bsonWriter.Serialize(doc);

            // storage in data pages - returns dataBlock address
            var dataBlock = data.Insert(col, bytes);

            // store id in a PK index [0 array]
            var pk = indexer.AddNode(col.PK, id, null);

            // do link between index <-> data block
            pk.DataBlock = dataBlock.Position;

            // for each index, insert new IndexNode
            foreach (var index in col.GetIndexes(false))
            {
                // for each index, get all keys (support now multi-key) - gets distinct values only
                // if index are unique, get single key only
                var expr = BsonExpression.Create(index.Expression);
                var keys = expr.Execute(doc, true);

                // do a loop with all keys (multi-key supported)
                foreach(var key in keys)
                {
                    // insert node
                    var node = indexer.AddNode(index, key, pk);

                    // link my index node to data block address
                    node.DataBlock = dataBlock.Position;
                }
            }
        }
    }
}