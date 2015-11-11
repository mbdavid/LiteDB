using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Remove all document based on a Query object. Returns removed document counts
        /// </summary>
        public int Delete(Query query)
        {
            if(query == null) throw new ArgumentNullException("query");

            lock(_locker)
            {
                // start transaction
                this.Database.Transaction.Begin();

                try
                {
                    var count = this.DeleteDocuments(query);

                    this.Database.Transaction.Commit();

                    return count;
                }
                catch (Exception ex)
                {
                    this.Database.Transaction.Rollback();
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Remove all document based on a LINQ query. Returns removed document counts
        /// </summary>
        public int Delete(Expression<Func<T, bool>> predicate)
        {
            return this.Delete(_visitor.Visit(predicate));
        }

        /// <summary>
        /// Remove an document in collection using Document Id - returns false if not found document
        /// </summary>
        public bool Delete(BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            return this.Delete(Query.EQ("_id", id)) > 0;
        }

        /// <summary>
        /// Internal implementation to delete a document - no trans, no locks
        /// </summary>
        internal int DeleteDocuments(Query query)
        {
            var col = this.GetCollectionPage(false);

            // no collection, no document - abort trans
            if (col == null) return 0;

            var count = 0;

            // find nodes
            var nodes = query.Run<T>(this);

            foreach (var node in nodes)
            {
                // read dataBlock 
                var dataBlock = this.Database.Data.Read(node.DataBlock, false);

                // lets remove all indexes that point to this in dataBlock
                foreach (var index in col.GetIndexes(true))
                {
                    this.Database.Indexer.Delete(index, dataBlock.IndexRef[index.Slot]);
                }

                // remove object data
                this.Database.Data.Delete(col, node.DataBlock);

                count++;
            }

            return count;
        }
    }
}
