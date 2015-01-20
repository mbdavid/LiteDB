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
        /// Drop a collection deleting all documents and indexes
        /// </summary>
        public virtual bool Drop()
        {
            // start transaction
            _engine.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(false);

                // if collection not exists, no drop
                if (col == null)
                {
                    _engine.Transaction.Abort();
                    return false;
                }

                // delete all data
                this.Delete(Query.All());

                // delete all pages/indexes heads
                _engine.Collections.Drop(col);

                _pageID = uint.MaxValue;

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
