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
        /// Insert a new document to this collection. Document Id must be a new value in collection
        /// </summary>
        public virtual void Insert(T doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            // gets document Id
            var id = BsonSerializer.GetIdValue(doc);

            if (id == null) throw new LiteException("Document Id can't be null");

            // serialize object
            var bytes = BsonSerializer.Serialize(doc);

            this.Database.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(true);

                // storage in data pages - returns dataBlock address
                var dataBlock = this.Database.Data.Insert(col, new IndexKey(id), bytes);

                // store id in a PK index [0 array]
                var pk = this.Database.Indexer.AddNode(col.PK, id);

                // do links between index <-> data block
                pk.DataBlock = dataBlock.Position;
                dataBlock.IndexRef[0] = pk.Position;

                // for each index, insert new IndexNode
                for (byte i = 1; i < col.Indexes.Length; i++)
                {
                    var index = col.Indexes[i];

                    if (!index.IsEmpty)
                    {
                        var key = BsonSerializer.GetFieldValue(doc, index.Field);

                        var node = this.Database.Indexer.AddNode(index, key);

                        // point my index to data object
                        node.DataBlock = dataBlock.Position;

                        // point my dataBlock
                        dataBlock.IndexRef[i] = node.Position;
                    }
                }

                this.Database.Transaction.Commit();
            }
            catch
            {
                this.Database.Transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection
        /// </summary>
        public virtual void Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            this.Database.Transaction.Begin();

            try
            {
                foreach (var doc in docs)
                {
                    this.Insert(doc);
                }

                this.Database.Transaction.Commit();
            }
            catch
            {
                this.Database.Transaction.Rollback();
                throw;
            }
        }
    }
}
