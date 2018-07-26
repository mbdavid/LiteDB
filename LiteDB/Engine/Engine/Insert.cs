using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Insert single document in collection. If document has no _id, use AutoId generation.
        /// </summary>
        public bool Insert(string collection, BsonDocument document, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            return this.Insert(collection, new[] { document }, autoId) > 0;
        }

        /// <summary>
        /// Insert all documents in collection. If document has no _id, use AutoId generation.
        /// </summary>
        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                var col = snapshot.CollectionPage;
                var count = 0;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                foreach (var doc in docs)
                {
                    transaction.Safepoint();

                    this.InsertDocument(snapshot, col, doc, autoId, indexer, data);

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
            // if no _id, use AutoId
            if (!doc.RawValue.TryGetValue("_id", out var id))
            {
                doc["_id"] = id =
                    autoId == BsonAutoId.ObjectId ? new BsonValue(ObjectId.NewObjectId()) :
                    autoId == BsonAutoId.Guid ? new BsonValue(Guid.NewGuid()) :
                    autoId == BsonAutoId.DateTime ? new BsonValue(DateTime.Now) :
                    this.GetSequence(col, snapshot, autoId);
            }
            else if(id.IsNumber)
            {
                this.SetSequence(col, id);
            }

            // test if _id is a valid type
            if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            {
                throw LiteException.InvalidDataType("_id", id);
            }

            // serialize object
            var stream = _bsonWriter.Serialize(doc);

            // storage in data pages - returns dataBlock address
            var dataBlock = data.Insert(col, stream);

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

        /// <summary>
        /// Collection last sequence cache
        /// </summary>
        private ConcurrentDictionary<string, long> _sequence = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Get lastest value from a _id collection and plus 1 - use _sequence cache
        /// </summary>
        private BsonValue GetSequence(CollectionPage col, Snapshot snapshot, BsonAutoId autoId)
        {
            var next = _sequence.AddOrUpdate(col.CollectionName, (s) =>
            {
                // add method
                var tail = col.GetIndex(0).TailNode;
                var head = col.GetIndex(0).HeadNode;

                // get tail page and previous page
                var tailPage = snapshot.GetPage<IndexPage>(tail.PageID);
                var node = tailPage.GetNode(tail.Index);
                var prevNode = node.Prev[0];

                if (prevNode == head)
                {
                    return 1;
                }
                else
                {
                    var lastPage = prevNode.PageID == tailPage.PageID ? tailPage : snapshot.GetPage<IndexPage>(prevNode.PageID);
                    var lastNode = lastPage.GetNode(prevNode.Index);

                    var lastKey = lastNode.Key;

                    if (lastKey.IsNumber == false)
                    {
                        throw new LiteException(0, $"It's not possible use AutoId={autoId} because last value from collection '{col.CollectionName}' is '{lastKey}' and is not a number");
                    }

                    return lastNode.Key.AsInt64 + 1;
                }
            },
            (s, value) =>
            {
                // update last value
                return value + 1;
            });

            return autoId == BsonAutoId.Int32 ?
                new BsonValue((int)next) :
                new BsonValue(next);
        }

        private void SetSequence(CollectionPage col, BsonValue lastId)
        {
            // TODO must update sequence when passed by user?
            //_sequence.TryUpdate()
        }
    }
}