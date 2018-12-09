using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        public BsonDocument Find_by_id(string collection, BsonValue value)
        {
            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Read, collection, false);
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                var node = indexer.Find(snapshot.CollectionPage.PK, value, false, 1);

                if (node == null) return null;

                var buffer = data.Read(node.DataBlock);

                using(var r = new BufferReader(buffer))
                {
                    var doc = r.ReadDocument();

                    return doc;
                }
            });
        }

        public bool Read_All_Docs(string collection, BsonValue id)
        {
            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Read, collection, false);
                var data = new DataService(snapshot);
                var count = 0;

                for(var slot = 0; slot < 5; slot++)
                {
                    var next = snapshot.CollectionPage.FreeDataPageID[slot];

                    while (next != uint.MaxValue)
                    {
                        var page = snapshot.GetPage<DataPage>(next);

                        foreach(var block in page.GetBlocks())
                        {
                            using (var r = new BufferReader(data.Read(block)))
                            {
                                var doc = r.ReadDocument();

                                if (doc["_id"] == id)
                                {
                                    Console.WriteLine("doc found: " + id.ToString() + " - " + doc.GetBytesCount(true));
                                }

                                count++;
                            }
                        }

                        next = page.NextPageID;

                        transaction.Safepoint();
                    }
                }

                Console.WriteLine($"Total documents in `{collection}`: {count}");
                return true;

            });
        }

        public bool Read_All_Docs_By_Index(string collection, BsonValue id)
        {
            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Read, collection, false);
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);
                var count = 0;

                for (var slot = 0; slot < 5; slot++)
                {
                    var nextIndexPage = snapshot.CollectionPage.FreeIndexPageID[slot];

                    while (nextIndexPage != uint.MaxValue)
                    {
                        var indexPage = snapshot.GetPage<IndexPage>(nextIndexPage);

                        foreach (var node in indexPage.GetNodes())
                        {
                            if (node.Key.IsMinValue || node.Key.IsMaxValue) continue;

                            using (var r = new BufferReader(data.Read(node.DataBlock)))
                            {
                                var doc = r.ReadDocument();

                                if (doc["_id"] == id)
                                {
                                    Console.WriteLine("doc found: " + id.ToString() + " - " + doc.GetBytesCount(true));
                                }

                                count++;
                            }
                        }

                        nextIndexPage = indexPage.NextPageID;

                        transaction.Safepoint();
                    }
                }

                Console.WriteLine($"Total nodes in `{collection}`: {count}");
                return true;

            });
        }

        public void Read_All_Docs_By_Index_Id(string collection, int start, int end)
        {
            this.AutoTransaction(transaction =>
            {
                var counter = 0;
                var snapshot = transaction.CreateSnapshot(LockMode.Read, collection, false);
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                for (var i = start; i <= end; i++)
                {
                    var node = indexer.Find(snapshot.CollectionPage.PK, i, false, 1);

                    if (node != null)
                    {
                        using (var r = new BufferReader(data.Read(node.DataBlock), false))
                        {
                            var doc = r.ReadDocument();

                            counter++;
                        }
                    }

                    transaction.Safepoint();
                }

                Console.WriteLine("FindIndexId: " + counter + " (range: " + start + " - " + end + ")");
                
                return true;

            });
        }

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
                    autoId == BsonAutoId.DateTime ? new BsonValue(DateTime.Now) :
                    this.GetSequence(snapshot, autoId);
            }
            else if(id.IsNumber)
            {
                // update memory sequence of numeric _id
//**                this.SetSequence(col, snapshot, id);
            }

            // test if _id is a valid type
            if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            {
                throw LiteException.InvalidDataType("_id", id);
            }

            // storage in data pages - returns dataBlock address
            var dataBlock = data.Insert(doc);

            //return;

            // for each index, insert new IndexNode
            foreach (var index in snapshot.CollectionPage.GetCollectionIndexes())
            {
                // for each index, get all keys (support now multi-key) - gets distinct values only
                // if index are unique, get single key only
                var expr = BsonExpression.Create(index.Expression);
                var keys = expr.Execute(doc, true);

                IndexNode last = null;

                // do a loop with all keys (multi-key supported)
                foreach(var key in keys)
                {
                    // insert node
                    var node = indexer.AddNode(index, key, dataBlock, last);
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
            throw new NotImplementedException();
            /*
            var next = _sequences.AddOrUpdate(col.CollectionName, (s) =>
            {
                var lastId = this.GetLastId(col, snapshot);

                // emtpy collection, return 1
                if (lastId.IsMinValue) return 1;

                // if lastId is not number, throw exception
                if (!lastId.IsNumber)
                {
                    throw new LiteException(0, $"It's not possible use AutoId={autoId} because '{col.CollectionName}' collection constains not only numbers in _id index ({lastId}).");
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
                new BsonValue(next);*/
        }

        /// <summary>
        /// Update sequence number with new _id passed by user, IF this number are higher than current last _id
        /// At this point, newId.Type is Number
        /// </summary>
        private void SetSequence(Snapshot snapshot, BsonValue newId)
        {
            throw new NotImplementedException();
            /*
            _sequences.AddOrUpdate(col.CollectionName, (s) =>
            {
                var lastId = this.GetLastId(col, snapshot);

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
            });*/
        }

        /// <summary>
        /// Get last _id index key from collection. Returns MinValue if collection are empty
        /// </summary>
        private BsonValue GetLastId(Snapshot snapshot)
        {
            throw new NotImplementedException();
            /*
            // add method
            var tail = col.GetIndex(0).TailNode;
            var head = col.GetIndex(0).HeadNode;

            // get tail page and previous page
            var tailPage = snapshot.GetPage<IndexPage>(tail.PageID);
            var node = tailPage.GetNode(tail.Index);
            var prevNode = node.Prev[0];

            if (prevNode == head)
            {
                return BsonValue.MinValue;
            }
            else
            {
                var lastPage = prevNode.PageID == tailPage.PageID ? tailPage : snapshot.GetPage<IndexPage>(prevNode.PageID);
                var lastNode = lastPage.GetNode(prevNode.Index);

                var lastKey = lastNode.Key;

                return lastKey;
            }*/
        }
    }
}