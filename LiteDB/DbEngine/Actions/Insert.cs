using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Implements insert documents in a collection - use a buffer to commit transaction in each buffer count
        /// </summary>
        public int InsertDocuments(string colName, IEnumerable<BsonDocument> docs, int bufferSize)
        {
            var enumerator = docs.GetEnumerator();
            var count = 0;
            var more = true;

            while (true)
            {
                var buffer = bufferSize;

                this.Transaction<int>(colName, true, (col) =>
                {
                    more = true;

                    while (buffer > 0 && (more = enumerator.MoveNext()))
                    {
                        this.InsertDocument(col, enumerator.Current);
                        buffer--;
                        count++;
                    }

                    return 0;
                });

                if (more == false)
                {
                    return count;
                }
            }
        }

        /// <summary>
        /// Insert a single document - must be used only inside "InsertDocuments"
        /// </summary>
        private void InsertDocument(CollectionPage col, BsonDocument doc)
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
        }
    }
}
