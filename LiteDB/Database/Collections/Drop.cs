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
        /// Drop a collection deleting all documents and indexes
        /// </summary>
        public virtual bool Drop()
        {
            // start transaction
            this.Database.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(false);

                // if collection not exists, no drop
                if (col == null)
                {
                    this.Database.Transaction.Abort();
                    return false;
                }

                // delete all data pages + indexes pages
                this.Database.Collections.Drop(col);

                _pageID = uint.MaxValue;

                this.Database.Transaction.Commit();

                return true;
            }
            catch
            {
                this.Database.Transaction.Rollback();
                throw;
            }
        }
    }
}
