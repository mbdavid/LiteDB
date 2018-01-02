using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string collection)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));

            return this.WriteTransaction(TransactionMode.Reserved, collection, false, trans =>
            {
                var col = trans.CollectionPage;

                if (col == null) return false;

                trans.Collection.Drop(col);

                return true;
            });
        }

        /// <summary>
        /// Rename a collection
        /// </summary>
        public bool RenameCollection(string collection, string newName)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newName));

            return this.WriteTransaction(TransactionMode.Reserved, collection, false, trans =>
            {
                var col = trans.CollectionPage;

                if (col == null) return false;

                trans.Collection.Rename(col, newName);

                return true;
            });
        }
    }
}