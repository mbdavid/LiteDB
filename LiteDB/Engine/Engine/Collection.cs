using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Create empty collection if not exits - return false if already exists
        /// </summary>
        public bool CreateCollection(string collection, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            // create snapshot for collection list page
            return transaction.CreateSnapshot(SnapshotMode.Write, collection, true, snapshot =>
            {
                // at this point, collection already
                // to know if is new or not, check if exists in header - if exists, are not new collection
                return !_header.Collections.ContainsKey(collection);
            });

        }
        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string collection, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            return transaction.CreateSnapshot(SnapshotMode.Write, collection, false, snapshot =>
            {
                var col = snapshot.CollectionPage;

                // if collection do not exist, just exit
                if (col == null) return false;

                var srv = snapshot.GetCollectionService();

                srv.Drop(col, transaction);

                return true;
            });
        }

        /// <summary>
        /// Rename a collection
        /// </summary>
        public bool RenameCollection(string collection, string newName, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newName));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            throw new NotImplementedException();

            //return transaction.CreateSnapshot(SnapshotMode.Write, collection, snapshot =>
            //{
            //    var srv = new CollectionService(snapshot);
            //    var col = srv.Get(collection);
            //
            //    if (col == null) return false;
            //
            //    srv.Rename(col, newName);
            //
            //    return true;
            //});
        }
    }
}