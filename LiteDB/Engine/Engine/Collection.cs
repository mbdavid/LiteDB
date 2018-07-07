using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Returns all collection inside datafile
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            return _header.Collections.Keys.AsEnumerable();
        }

        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string collection)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
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
        public bool RenameCollection(string collection, string newName)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newName));
            if (collection.Equals(newName, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("To rename a collection the new name must be different from current collection name");
            if (_shutdown) throw LiteException.DatabaseShutdown();

            // rename collection is possible only in exclusive transaction for this
            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("RenameCollection", TransactionState.Active);

            return AutoTransaction(transaction =>
            {
                var currentSnapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
                var newSnapshot = transaction.CreateSnapshot(LockMode.Write, newName, false);

                // check if has space in header if new name are larger than current name
                if (newName.Length > collection.Length)
                {
                    _header.CheckCollectionsSize(newName.Substring(0, newName.Length - collection.Length));
                }

                // checks if do not already exists this collection name
                if (_header.Collections.ContainsKey(newName))
                {
                    throw LiteException.AlreadyExistsCollectionName(newName);
                }

                var col = currentSnapshot.CollectionPage;

                if (col == null) return false;

                // rename collection and set page as dirty
                col.CollectionName = newName;
                currentSnapshot.SetDirty(col);

                transaction.Pages.DeletedCollections.Add(collection);
                transaction.Pages.NewCollections.Add(newName, col.PageID);

                return true;
            });
        }
    }
}