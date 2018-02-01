using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public static class Extensions
    {
        public static void EnsureIndex(this LiteEngine engine, string collection, string name, string expr, bool unique = false)
        {
            using (var t = engine.BeginTrans())
            {
                engine.EnsureIndex(collection, name, new BsonExpression(expr), unique, t);

                t.Commit();
            }
        }

        public static int Insert(this LiteEngine engine, string collection, IEnumerable<BsonDocument> docs)
        {
            try
            {
                using (var t = engine.BeginTrans())
                {
                    var count = engine.Insert(collection, docs, BsonAutoId.Int32, t);

                    t.Commit();

                    return count;
                }
            }
            catch(LiteException ex) when (ex.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
            {
                return 0;
            }
        }

        public static void DropCollection(this LiteEngine engine, string collection)
        {
            using (var t = engine.BeginTrans())
            {
                engine.DropCollection(collection, t);

                t.Commit();
            }
        }

        public static Task<int> InsertAsync(this LiteEngine engine, string collection, IEnumerable<BsonDocument> docs)
        {
            return Task.Run<int>(() => Insert(engine, collection, docs));
        }

        public static BsonDocument FindById(this LiteEngine engine, string collection, BsonValue id)
        {
            using (var t = engine.BeginTrans())
            {
                var q = new Query
                {
                    Index = Index.EQ("_id", id)
                };

                return engine.Find(collection, q, t).FirstOrDefault();
            }
        }

        public static List<BsonDocument> Find(this LiteEngine engine, string collection, Query query)
        {
            using (var t = engine.BeginTrans())
            {
                return engine.Find(collection, query, t).ToList();
            }
        }
    }
}