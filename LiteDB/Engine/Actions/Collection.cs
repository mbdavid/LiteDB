using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class LiteEngine : IDisposable
    {
        /// <summary>
        /// Returns all collection inside datafile
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            _transaction.AvoidDirtyRead();

            return _collections.GetAll().Select(x => x.CollectionName);
        }

        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string colName)
        {
            // get collection page
            var col = this.GetCollectionPage(colName, false);

            if(col == null) return false;

            _collections.Drop(col);

            return true;
        }

        /// <summary>
        /// Rename a collection
        /// </summary>
        public bool RenameCollection(string colName, string newName)
        {
            return this.Transaction<bool>(colName, false, (col) =>
            {
                if(col == null) return false;

                // change collection name and save
                col.CollectionName = newName;
                _pager.SetDirty(col);

                // remove from collection cache
                _collectionPages.Remove(colName);

                return true;
            });
        }
    }
}
