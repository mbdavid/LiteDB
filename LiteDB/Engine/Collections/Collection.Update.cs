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
        /// Update object in collection
        /// </summary>
        public virtual bool Update(object id, T value)
        {
            var col = this.GetCollectionPage();

            // find indexNode from pk index
            var indexNode = _engine.Indexer.FindOne(col.PK, id);

            if (indexNode == null) return false;

            // serialize object
            var bytes = BsonSerializer.Serialize(value);

            if (bytes.Length > BsonDocument.MAX_DOCUMENT_SIZE)
                throw new LiteDBException("Object exceed limit of " + Math.Truncate(BsonDocument.MAX_DOCUMENT_SIZE / 1024m) + " Kb");

            // start transaction - if clear cache, get again collection page
            if (_engine.Transaction.Begin())
            {
                col = this.GetCollectionPage();
            }

            try
            {
                // update data storage
                var dataBlock = _engine.Data.Update(col, indexNode.DataBlock, bytes);

                // delete/insert indexes - do not touch on PK
                for (byte i = 1; i < col.Indexes.Length; i++)
                {
                    var index = col.Indexes[i];

                    if (!index.IsEmpty)
                    {
                        var key = BsonSerializer.GetValueField(value, index.Field);

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
