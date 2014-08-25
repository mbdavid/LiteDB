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
        /// Delete a item - returns false if not found id
        /// </summary>
        public virtual bool Delete(object id)
        {
            var col = this.GetCollectionPage();

            // find indexNode using PK index
            var node = _engine.Indexer.FindOne(col.PK, id);

            if (node == null) return false;

            // start transaction - if clear cache, get again collection page
            if (_engine.Transaction.Begin())
            {
                col = this.GetCollectionPage();
            }

            try
            {
                this.Delete(col, node);

                _engine.Transaction.Commit();

                return true;
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }
        }

        public virtual int Delete(Query query)
        {
            var count = 0;
            var col = this.GetCollectionPage();

            // find nodes
            var nodes = query.Execute(_engine, col);

            // start transaction - if clear cache, get again collection page
            if (_engine.Transaction.Begin())
            {
                col = this.GetCollectionPage();
            }

            try
            {
                foreach (var node in nodes)
                {
                    this.Delete(col, node);
                    count++;
                }

                _engine.Transaction.Commit();

                return count;
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }
        }

        internal virtual void Delete(CollectionPage col, IndexNode node)
        {
            // read dataBlock 
            var dataBlock = _engine.Data.Read(node.DataBlock, false);

            // lets remove all indexes that point to this in dataBlock
            for (byte i = 0; i < col.Indexes.Length; i++)
            {
                var index = col.Indexes[i];

                if (!index.IsEmpty)
                {
                    _engine.Indexer.Delete(index, dataBlock.IndexRef[i]);
                }
            }

            // remove object data
            _engine.Data.Delete(col, node.DataBlock);
        }
    }
}
