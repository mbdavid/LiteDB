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
        public bool Rename(string newName)
        {
            this.Database.Transaction.Begin();

            try
            {
                // get collection page
                var col = this.GetCollectionPage(false);

                if (col == null)
                {
                    this.Database.Transaction.Abort();
                    return false;
                }

                // change collection name
                col.CollectionName = newName;

                // set page as dirty
                this.Database.Pager.SetDirty(col);

                this.Database.Transaction.Commit();
            }
            catch
            {
                this.Database.Transaction.Rollback();
                throw;
            }

            return true;
        }
    }
}
