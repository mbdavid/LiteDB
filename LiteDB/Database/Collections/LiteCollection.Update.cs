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
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public virtual bool Update(T doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            // gets document Id
            var id = BsonSerializer.GetIdValue(doc);

            if (id == null) throw new LiteException("Document Id can't be null");

            // serialize object
            var bytes = BsonSerializer.Serialize(doc);

            // start transaction
            this.Database.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(false);

                // if no collection, no updates
                if (col == null)
                {
                    this.Database.Transaction.Abort();
                    return false;
                }

                // find indexNode from pk index
                var indexNode = this.Database.Indexer.FindOne(col.PK, id);

                // if not found document, no updates
                if (indexNode == null)
                {
                    this.Database.Transaction.Abort();
                    return false;
                }

                // update data storage
                var dataBlock = this.Database.Data.Update(col, indexNode.DataBlock, bytes);

                // delete/insert indexes - do not touch on PK
                for (byte i = 1; i < col.Indexes.Length; i++)
                {
                    var index = col.Indexes[i];

                    if (!index.IsEmpty)
                    {
                        var key = BsonSerializer.GetFieldValue(doc, index.Field);

                        var node = this.Database.Indexer.GetNode(dataBlock.IndexRef[i]);

                        // check if my index node was changed
                        if (node.Key.CompareTo(new IndexKey(key)) != 0)
                        {
                            // remove old index node
                            this.Database.Indexer.Delete(index, node.Position);

                            // and add a new one
                            var newNode = this.Database.Indexer.AddNode(index, key);

                            // point my index to data object
                            newNode.DataBlock = dataBlock.Position;

                            // point my dataBlock
                            dataBlock.IndexRef[i] = newNode.Position;

                            dataBlock.Page.IsDirty = true;
                        }
                    }
                }

                this.Database.Transaction.Commit();

                return true;
            }
            catch (Exception ex)
            {
                this.Database.Transaction.Rollback();
                throw ex;
            }
        }
    }
}
