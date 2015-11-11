using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Insert a new document to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public BsonValue Insert(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            lock (_locker)
            {
                this.Database.Transaction.Begin();

                try
                {
                    var id = this.InsertDocument(document);

                    this.Database.Transaction.Commit();

                    return id;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection
        /// </summary>
        public int Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            var count = 0;

            lock (_locker)
            {
                this.Database.Transaction.Begin();

                try
                {
                    foreach (var doc in docs)
                    {
                        this.InsertDocument(doc);
                        count++;
                    }

                    this.Database.Transaction.Commit();

                    return count;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Internal implementation of Insert a document. Returns document _id - no trans, no locks
        /// </summary>
        private BsonValue InsertDocument(T document)
        {
            // set an id value if document object needs
            this.Database.Mapper.SetAutoId(document, this.GetBsonCollection());

            var doc = this.Database.Mapper.ToDocument(document);

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

            // serialize object
            var bytes = BsonSerializer.Serialize(doc);

            var col = this.GetCollectionPage(true);

            // storage in data pages - returns dataBlock address
            var dataBlock = this.Database.Data.Insert(col, bytes);

            // store id in a PK index [0 array]
            var pk = this.Database.Indexer.AddNode(col.PK, id);

            // do links between index <-> data block
            pk.DataBlock = dataBlock.Position;
            dataBlock.IndexRef[0] = pk.Position;

            // for each index, insert new IndexNode
            foreach (var index in col.GetIndexes(false))
            {
                var key = doc.Get(index.Field);

                var node = this.Database.Indexer.AddNode(index, key);

                // point my index to data object
                node.DataBlock = dataBlock.Position;

                // point my dataBlock
                dataBlock.IndexRef[index.Slot] = node.Position;
            }

            return id;
        }
    }
}
