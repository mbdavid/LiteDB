using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement update command to a document inside a collection. Return number of documents updated
        /// </summary>
        public int Update(string collection, IEnumerable<BsonDocument> docs)
        {
            throw new NotImplementedException(); /*
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);
                var count = 0;

                foreach (var doc in docs)
                {
                    transaction.Safepoint();

                    if (this.UpdateDocument(snapshot, col, doc, indexer, data))
                    {
                        count++;
                    }
                }

                return count;
            });*/
        }

        /// <summary>
        /// Update documents using extend expression to extend found document using predicate
        /// </summary>
        public int UpdateMany(string collection, BsonExpression extend, BsonExpression predicate)
        {
            throw new NotImplementedException(); /*
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (extend == null) throw new ArgumentNullException(nameof(extend));

            return this.AutoTransaction(transaction =>
            {
                return this.Update(collection, ExtendDocs());

                IEnumerable<BsonDocument> ExtendDocs()
                {
                    var q = new QueryDefinition { Select = "$", ForUpdate = true };

                    if (predicate != null)
                    {
                        q.Where.Add(predicate);
                    }

                    using (var reader = this.Query(collection, q))
                    {
                        while(reader.Read())
                        {
                            var doc = reader.Current.AsDocument;

                            var id = doc["_id"];
                            var result = extend.Execute(doc, true).First();

                            if (!result.IsDocument) throw new ArgumentException("Extend expression must return a document", nameof(extend));

                            var output = doc.Extend(result.AsDocument);

                            // be sure result document will contain same _id as current doc
                            if (output.TryGetValue("_id", out var newId))
                            {
                                if (newId != id) throw LiteException.InvalidUpdateField("_id");
                            }
                            else
                            {
                                output["_id"] = id;
                            }

                            yield return output;
                        }
                    }
                }
            });*/
        }

        /*
        /// <summary>
        /// Implement internal update document
        /// </summary>
        private bool UpdateDocument(Snapshot snapshot, CollectionPage col, BsonDocument doc, IndexService indexer, DataService data)
        {
            // normalize id before find
            var id = doc["_id"];
            
            // validate id for null, min/max values
            if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            {
                throw LiteException.InvalidDataType("_id", id);
            }
            
            // find indexNode from pk index
            var pkNode = indexer.Find(col.PK, id, false, LiteDB.Query.Ascending);
            
            // if not found document, no updates
            if (pkNode == null) return false;

            // serialize document in bytes
            var stream = _bsonWriter.Serialize(doc);
            
            // update data storage
            var dataBlock = data.Update(col, pkNode.DataBlock, stream);
            
            // get all non-pk index nodes from this data block
            var allNodes = indexer.GetNodeList(pkNode, false).ToArray();
            
            // delete/insert indexes - do not touch on PK
            foreach (var index in col.GetIndexes(false))
            {
                var expr = BsonExpression.Create(index.Expression);
            
                // getting all keys do check
                var keys = expr.Execute(doc).ToArray();
            
                // get a list of to delete nodes (using ToArray to resolve now)
                var toDelete = allNodes
                    .Where(x => x.Slot == index.Slot && !keys.Any(k => k == x.Key))
                    .ToArray();
            
                // get a list of to insert nodes (using ToArray to resolve now)
                var toInsert = keys
                    .Where(x => !allNodes.Any(k => k.Slot == index.Slot && k.Key == x))
                    .ToArray();
            
                // delete changed index nodes
                foreach (var node in toDelete)
                {
                    indexer.Delete(index, node.Position);
                }
            
                // insert new nodes
                foreach (var key in toInsert)
                {
                    // and add a new one
                    var node = indexer.AddNode(index, key, pkNode);
            
                    // link my node to data block
                    node.DataBlock = dataBlock.Position;
                }
            }

            return true;
        }*/
    }
}