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
            return transaction.CreateSnapshot(snapshot =>
            {
                var srv = new CollectionService(snapshot);

                // already contains this collection
                if (srv.Get(collection) != null) return false;

                // otherwise, create new
                srv.Add(collection);

                return true;
            });

        }
        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string collection, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            // lock collection list page
            transaction.CreateSnapshot(snapshot => true);

            // and now lock collection
            return transaction.CreateSnapshot(SnapshotMode.Write, collection, false, snapshot =>
            {
                // if collection do not exist, just exit
                if (snapshot.CollectionPage == null) return false;

                var srv = new CollectionService(snapshot);

                srv.Drop(snapshot.CollectionPage);

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

            // lock both collection names
            transaction.CreateSnapshot(SnapshotMode.Write, collection, false, snapshot => true);
            transaction.CreateSnapshot(SnapshotMode.Write, newName, false, snapshot => true);

            // and now, lock collection list page
            return transaction.CreateSnapshot(snapshot =>
            {
                var srv = new CollectionService(snapshot);
                var col = srv.Get(collection);

                if (col == null) return false;

                srv.Rename(col, newName);

                return true;
            });
        }
    }
}