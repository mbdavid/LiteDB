using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Returns all collection inside datafile
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            using (var trans = this.BeginTrans())
            {
                var header = trans.GetPage<HeaderPage>(0);

                return header.CollectionPages.Keys.AsEnumerable();
            }
        }

        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string collection)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));

            using (var trans = this.BeginTrans())
            {
                var col = trans.Collection.Get(collection);

                if (col == null) return false;

                // lock collection
                trans.WriteLock(collection);

                trans.Collection.Drop(col);

                // persist changes
                trans.Commit();

                return true;
            }
        }

        /// <summary>
        /// Rename a collection
        /// </summary>
        public bool RenameCollection(string collection, string newName)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newName));

            using (var trans = this.BeginTrans())
            {
                var col = trans.Collection.Get(collection);

                if (col == null) return false;

                // lock collection
                trans.WriteLock(collection);

                trans.Collection.Rename(col, newName);

                // persist changes
                trans.Commit();

                return true;
            };
        }
    }
}