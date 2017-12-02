using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class Paging
    {
        static string filename = Path.Combine(Path.GetTempPath(), "file_paging.db");

        public static void StartTest()
        {
            File.Delete(filename);

            using (var db = new LiteEngine(filename))
            {
                Console.WriteLine("Populating...");

                // pouplate collection
                db.InsertBulk("col", Populate(75000));

                // create indexes
                db.EnsureIndex("col", "name");
                db.EnsureIndex("col", "age");

                // query by age
                var query = Query.EQ("age", 22);

                // show count result
                Console.WriteLine("Result count: " + db.Count("col", query));

                var input = "0";

                while(input != "")
                {
                    var skip = Convert.ToInt32(input);
                    var limit = 10;

                    var timer = new Stopwatch();

                    timer.Start();

                    var result = db.FindSort(
                        "col", 
                        query, 
                        "$.name", 
                        Query.Ascending, 
                        skip, 
                        limit);

                    timer.Stop();

                    Console.WriteLine("\n\nSkip docs: " + skip + " (ms: " + timer.ElapsedMilliseconds + ")");

                    foreach (var doc in result)
                    {
                        Console.WriteLine(doc["_id"].AsString.PadRight(6) + " - " + doc["name"].AsString + "  -> " + doc["age"].AsInt32);
                    }

                    Console.Write("\nEnter new skip index: ");
                    input = Console.ReadLine();
                }
            }
        }

        static IEnumerable<BsonDocument> Populate(int count)
        {
            var rnd = new Random();

            for(var i = 1; i <= count; i++)
            {
                yield return new BsonDocument
                {
                    ["_id"] = i,
                    ["name"] = Guid.NewGuid().ToString("d"),
                    ["age"] = rnd.Next(18, 40)
                    //["long"] = Guid.NewGuid().ToString("d").PadRight(1000, '-')
                };
            }
        }
    }

    public static class PagingExtensions
    {
        // Using copy all document solution
        public static List<BsonDocument> FindSort_TempCollectionDisk(this LiteEngine db, string collection, Query query, string orderBy, int order, int skip, int limit)
        {
            var tmp = "tmp_" + Guid.NewGuid().ToString().Substring(0, 5);

            // create index in tmp collection on orderBy column
            db.EnsureIndex(tmp, "orderBy", orderBy);

            // insert unsorted result inside a temp collection
            db.InsertBulk(tmp, db.Find(collection, query));

            // now, get all documents in temp using orderBy expr index with skip/limit 
            var sorted = db.Find(tmp, Query.All("orderBy", order), skip, limit);

            // convert docs to T entity
            var result = sorted.ToList();

            // drop temp collection
            db.DropCollection(tmp);

            return result;
        }

        // coping only _id, orderColumn solution
        public static List<BsonDocument> FindSort_TempCollectionMemory(this LiteEngine db, string collection, Query query, string orderBy, int order, int skip, int limit)
        {
            var expr = new BsonExpression(orderBy);
            var disk = new StreamDiskService(new MemoryStream(), true);

            // create in-memory database with large cache area
            using (var engine = new LiteEngine(disk, cacheSize: 200000))
            {
                // create index in tmp collection on orderBy column
                engine.EnsureIndex("tmp", "a");

                // insert unsorted result inside a temp collection - only _id value and orderBy column
                engine.Insert("tmp", db.Find(collection, query).Select(doc => new BsonDocument
                {
                    ["_id"] = doc["_id"],
                    ["a"] = expr.Execute(doc, true).First()
                }));

                // now, get all documents in temp orderBy expr with skip/limit 
                var sorted = engine.Find("tmp", Query.All("a", order), skip, limit);

                // for each sorted doc, find in real collection
                var result = sorted.Select(x => db.FindById(collection, x["_id"])).ToList();

                return result;
            }
        }
    }
}