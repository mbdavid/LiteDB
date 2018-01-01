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
            using (var trans = this.NewTransaction(TransactionMode.Read, null))
            {
                var colList = trans.Pager.GetPage<CollectionListPage>(1);

                return colList.GetAll().Select(x => x.Key);
            }
        }

        /// <summary>
        /// Drop collection including all documents, indexes and extended pages
        /// </summary>
        public bool DropCollection(string collection)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));

            using (var trans = this.NewTransaction(TransactionMode.Reserved, collection))
            {
                var col = trans.CollectionPage;

                if (col == null) return false;

                trans.Collection.Drop(col);

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

            using (var trans = this.NewTransaction(TransactionMode.Reserved, collection))
            {
                var col = trans.CollectionPage;

                if (col == null) return false;

                trans.Collection.Rename(col, newName);

                trans.Commit();

                return true;
            };
        }
    }
}