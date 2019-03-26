using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Insert all documents in collection. If document has no _id, use AutoId generation.
        /// </summary>
        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                var count = 0;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                foreach (var doc in docs)
                {
                    transaction.Safepoint();

                    this.InsertDocument(snapshot, doc, autoId, indexer, data);

                    count++;
                }

                return count;
            });
        }

        /// <summary>
        /// Internal implementation of insert a document
        /// </summary>
        private void InsertDocument(Snapshot snapshot, BsonDocument doc, BsonAutoId autoId, IndexService indexer, DataService data)
        {
            // if no _id, use AutoId
            if (!doc.RawValue.TryGetValue("_id", out var id))
            {
                doc["_id"] = id =
                    autoId == BsonAutoId.ObjectId ? new BsonValue(ObjectId.NewObjectId()) :
                    autoId == BsonAutoId.Guid ? new BsonValue(Guid.NewGuid()) :
                    this.GetSequence(snapshot, autoId);
            }
            else if(id.IsNumber)
            {
                // update memory sequence of numeric _id
                this.SetSequence(snapshot, id);
            }

            // test if _id is a valid type
            if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            {
                throw LiteException.InvalidDataType("_id", id);
            }

            // storage in data pages - returns dataBlock address
            var dataBlock = data.Insert(doc);

            //return;

            IndexNode last = null;

            // for each index, insert new IndexNode
            foreach (var index in snapshot.CollectionPage.GetCollectionIndexes())
            {
                // for each index, get all keys (support now multi-key) - gets distinct values only
                // if index are unique, get single key only
                var keys = index.BsonExpr.Execute(doc, true);

                // do a loop with all keys (multi-key supported)
                foreach(var key in keys)
                {
                    // insert node
                    var node = indexer.AddNode(index, key, dataBlock, last);

                    last = node;
                }
            }
        }

        /// <summary>
        /// Collection last sequence cache
        /// </summary>
        private ConcurrentDictionary<string, long> _sequences = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Get lastest value from a _id collection and plus 1 - use _sequence cache
        /// </summary>
        private BsonValue GetSequence(Snapshot snapshot, BsonAutoId autoId)
        {
            var next = _sequences.AddOrUpdate(snapshot.CollectionName, (s) =>
            {
                var lastId = this.GetLastId(snapshot);

                // emtpy collection, return 1
                if (lastId.IsMinValue) return 1;

                // if lastId is not number, throw exception
                if (!lastId.IsNumber)
                {
                    throw new LiteException(0, $"It's not possible use AutoId={autoId} because '{snapshot.CollectionName}' collection contains not only numbers in _id index ({lastId}).");
                }

                // return nextId
                return lastId.AsInt64 + 1;
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

        /// <summary>
        /// Update sequence number with new _id passed by user, IF this number are higher than current last _id
        /// At this point, newId.Type is Number
        /// </summary>
        private void SetSequence(Snapshot snapshot, BsonValue newId)
        {
            _sequences.AddOrUpdate(snapshot.CollectionName, (s) =>
            {
                var lastId = this.GetLastId(snapshot);

                // create new collection based with max value between last _id index key or new passed _id
                if (lastId.IsNumber)
                {
                    return Math.Max(lastId.AsInt64, newId.AsInt64);
                }
                else
                {
                    // if collection last _id is not an number (is empty collection or contains another data type _id)
                    // use newId
                    return newId.AsInt64;
                }

            }, (s, value) =>
            {
                // return max value between current sequence value vs new inserted value
                return Math.Max(value, newId.AsInt64);
            });
        }

        /// <summary>
        /// Get last _id index key from collection. Returns MinValue if collection are empty
        /// </summary>
        private BsonValue GetLastId(Snapshot snapshot)
        {
            var pk = snapshot.CollectionPage.PK;

            // get tail page and previous page
            var tailPage = snapshot.GetPage<IndexPage>(pk.Tail.PageID);
            var node = tailPage.GetIndexNode(pk.Tail.Index);
            var prevNode = node.Prev[0];

            if (prevNode == pk.Head)
            {
                return BsonValue.MinValue;
            }
            else
            {
                var lastPage = prevNode.PageID == tailPage.PageID ? tailPage : snapshot.GetPage<IndexPage>(prevNode.PageID);
                var lastNode = lastPage.GetIndexNode(prevNode.Index);

                var lastKey = lastNode.Key;

                return lastKey;
            }
        }
    }
}