using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
        where T : new()
    {
        private uint _pageID;
        private List<Action<T>> _includes;
        private QueryVisitor<T> _visitor;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets database object reference
        /// </summary>
        public LiteDatabase Database { get; private set; }

        internal LiteCollection(LiteDatabase db, string name)
        {
            this.Name = name;
            this.Database = db;
            _pageID = uint.MaxValue;
            _visitor = new QueryVisitor<T>(db.Mapper);
            _includes = new List<Action<T>>();
        }

        /// <summary>
        /// Get the collection page only when nedded. Gets from cache always to garantee that wil be the last (in case of _clearCache will get a new one)
        /// </summary>
        internal CollectionPage GetCollectionPage(bool addIfNotExits)
        {
            // when a operation is read-only, request collection page without add new one
            // use this moment to check if data file was changed (if in transaction, do nothing)
            if (addIfNotExits == false)
            {
                this.Database.Transaction.AvoidDirtyRead();
            }

            // _pageID never change, even if data file was changed
            if (_pageID == uint.MaxValue)
            {
                var col = this.Database.Collections.Get(this.Name);

                if (col == null)
                {
                    // create a new collection only if 
                    if (addIfNotExits)
                    {
                        col = this.Database.Collections.Add(this.Name);
                    }
                    else
                    {
                        return null;
                    }
                }

                _pageID = col.PageID;

                return col;
            }

            return this.Database.Pager.GetPage<CollectionPage>(_pageID);
        }

        /// <summary>
        /// Returns a new instance of this collection but using BsonDocument as T - Copy _pageID to avoid new collection page search
        /// </summary>
        internal LiteCollection<BsonDocument> GetBsonCollection()
        {
            var col = new LiteCollection<BsonDocument>(this.Database, this.Name);

            col._pageID = _pageID;

            return col;
        }
    }
}
