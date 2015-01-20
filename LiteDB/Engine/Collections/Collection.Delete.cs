using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class Collection<T>
    {
        /// <summary>
        /// Delete a document in collection using Document Id - returns false if not found document
        /// </summary>
        public virtual bool Delete(object id)
        {
            // start transaction
            _engine.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(false);

                // if collection not exists, document do not exists too
                if (col == null)
                {
                    _engine.Transaction.Abort();
                    return false;
                }

                // find indexNode using PK index
                var node = _engine.Indexer.FindOne(col.PK, id);

                // if not found, abort transaction and returns false
                if (node == null)
                {
                    _engine.Transaction.Abort();
                    return false;
                }

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
            // start transaction
            _engine.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(false);

                // no collection, no document - abort trans
                if (col == null)
                {
                    _engine.Transaction.Abort();
                    return 0;
                }

                var count = 0;

                // find nodes
                var nodes = query.Run(_engine, col);

                foreach (var node in nodes)
                {
                    this.Delete(col, node);
                    count++;
                }

                // no deletes, just abort transaction (no writes)
                if (count == 0)
                {
                    _engine.Transaction.Abort();
                    return 0;
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

        /// <summary>
        /// Delete document based on a LINQ query.
        /// </summary>
        public virtual int Delete(Expression<Func<T, bool>> predicate)
        {
            return this.Delete(QueryVisitor.Visit(predicate));
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
