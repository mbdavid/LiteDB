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

            _log.Command($"drop collection", collection);

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(SnapshotMode.Write, collection, false);
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

            _log.Command($"rename collection `{collection}` to `{newName}`");

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