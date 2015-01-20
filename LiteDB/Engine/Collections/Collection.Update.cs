using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class Collection<T>
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
            _engine.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(false);

                // if no collection, no updates
                if (col == null)
                {
                    _engine.Transaction.Abort();
                    return false;
                }

                // find indexNode from pk index
                var indexNode = _engine.Indexer.FindOne(col.PK, id);

                // if not found document, no updates
                if (indexNode == null)
                {
                    _engine.Transaction.Abort();
                    return false;
                }

                // update data storage
                var dataBlock = _engine.Data.Update(col, indexNode.DataBlock, bytes);

                // delete/insert indexes - do not touch on PK
                for (byte i = 1; i < col.Indexes.Length; i++)
                {
                    var index = col.Indexes[i];

                    if (!index.IsEmpty)
                    {
                        var key = BsonSerializer.GetFieldValue(doc, index.Field);

                        var node = _engine.Indexer.GetNode(dataBlock.IndexRef[i]);

                        // check if my index node was changed
                        if (node.Key.CompareTo(new IndexKey(key)) != 0)
                        {
                            // remove old index node
                            _engine.Indexer.Delete(index, node.Position);

                            // and add a new one
                            var newNode = _engine.Indexer.AddNode(index, key);

                            // point my index to data object
                            newNode.DataBlock = dataBlock.Position;

                            // point my dataBlock
                            dataBlock.IndexRef[i] = newNode.Position;

                            dataBlock.Page.IsDirty = true;
                        }
                    }
                }

                _engine.Transaction.Commit();

                return true;
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }
        }
    }
}
