using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get internal information about database. Can filter collections
        /// </summary>
        public BsonDocument Info()
        {
            using (var trans = this.BeginTrans())
            {
                var header = trans.GetPage<HeaderPage>(0);
                var collections = new BsonArray();

                foreach(var colName in header.CollectionPages.Keys)
                {
                    var col = trans.Collection.Get(colName);

                    var colDoc = new BsonDocument
                    {
                        { "name", col.CollectionName },
                        { "pageID", (double)col.PageID },
                        { "count", col.DocumentCount },
                        { "sequence", col.Sequence },
                        { "indexes", new BsonArray(
                            col.Indexes.Where(x => !x.IsEmpty).Select(i => new BsonDocument
                            {
                                {  "slot", i.Slot },
                                {  "name", i.Name },
                                {  "expression", i.Expression },
                                {  "unique", i.Unique }
//                                {  "maxLevel", BsonValue.Int32(i.MaxLevel) }
                            }))
                        }
                    };

                    collections.Add(colDoc);
                }

                return new BsonDocument
                {
                    { "userVersion", (int)header.UserVersion },
                    { "lastPageID", (int)header.LastPageID },
                    { "fileSize", BasePage.GetPagePosition((int)header.LastPageID + 1) },
                    { "collections", collections }
                };
            }
        }
    }
}