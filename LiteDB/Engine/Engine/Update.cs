using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement update command to a document inside a collection. Return number of documents updated
        /// </summary>
        public int Update(string collection, IEnumerable<BsonDocument> docs)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));
            return 0;
            //return this.WriteTransaction(TransactionMode.Write, collection, false, trans =>
            //{
            //    var col = trans.CollectionPage;
            //
            //    // no collection, no updates
            //    if (col == null) return 0;
            //
            //    var count = 0;
            //
            //    foreach (var doc in docs)
            //    {
            //        if (this.UpdateDocument(trans, col, doc))
            //        {
            //            count++;
            //        }
            //
            //        trans.Safepoint();
            //    }
            //
            //    return count;
            //});
        }

        /// <summary>
        /// Implement internal update document
        /// </summary>
        private bool UpdateDocument(Snapshot snapshot, CollectionPage col, BsonDocument doc)
        {
            //// normalize id before find
            //var id = doc["_id"];
            //
            //// validate id for null, min/max values
            //if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            //{
            //    throw LiteException.InvalidDataType("_id", id);
            //}
            //
            //// find indexNode from pk index
            //var pkNode = snapshot.Indexer.Find(col.PK, id, false, Query.Ascending);
            //
            //// if not found document, no updates
            //if (pkNode == null) return false;
            //
            //// serialize document in bytes
            //var bytes = _bsonWriter.Serialize(doc);
            //
            //// update data storage
            //var dataBlock = snapshot.Data.Update(col, pkNode.DataBlock, bytes);
            //
            //// get all non-pk index nodes from this data block
            //var allNodes = snapshot.Indexer.GetNodeList(pkNode, false).ToArray();
            //
            //// delete/insert indexes - do not touch on PK
            //foreach (var index in col.GetIndexes(false))
            //{
            //    var expr = new BsonExpression(index.Expression);
            //
            //    // getting all keys do check
            //    var keys = expr.Execute(doc).ToArray();
            //
            //    // get a list of to delete nodes (using ToArray to resolve now)
            //    var toDelete = allNodes
            //        .Where(x => x.Slot == index.Slot && !keys.Any(k => k == x.Key))
            //        .ToArray();
            //
            //    // get a list of to insert nodes (using ToArray to resolve now)
            //    var toInsert = keys
            //        .Where(x => !allNodes.Any(k => k.Slot == index.Slot && k.Key == x))
            //        .ToArray();
            //
            //    // delete changed index nodes
            //    foreach (var node in toDelete)
            //    {
            //        snapshot.Indexer.Delete(index, node.Position);
            //    }
            //
            //    // insert new nodes
            //    foreach (var key in toInsert)
            //    {
            //        // and add a new one
            //        var node = snapshot.Indexer.AddNode(index, key, pkNode);
            //
            //        // link my node to data block
            //        node.DataBlock = dataBlock.Position;
            //    }
            //}

            return true;
        }
    }
}