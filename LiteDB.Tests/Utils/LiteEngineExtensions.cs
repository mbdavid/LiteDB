using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public static class LiteEngineExtensions
    {
        public static int Insert(this LiteEngine engine, string collection, BsonDocument doc, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            return engine.Insert(collection, new BsonDocument[] { doc }, autoId);
        }

        public static int Update(this LiteEngine engine, string collection, BsonDocument doc)
        {
            return engine.Update(collection, new BsonDocument[] { doc });
        }

        public static List<BsonDocument> Find(this LiteEngine engine, string collection, BsonExpression where)
        {
            var q = new LiteDB.Query();

            if (where != null)
            {
                q.Where.Add(where);
            }

            var docs = new List<BsonDocument>();

            using (var r = engine.Query(collection, q))
            {
                while (r.Read())
                {
                    docs.Add(r.Current.AsDocument);
                }
            }

            return docs;
        }

        public static BsonDocument GetPageLog(this LiteEngine engine, int pageID)
        {
            return engine.Find("$dump_log", "pageID = " + pageID).Last();
        }
    }
}