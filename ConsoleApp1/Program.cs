using LiteDB;
using LiteDB.Engine;

using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        const string PATH = @"C:\Dev\Git\Data\free_list_page_test.db";

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing database...");

            File.Delete(PATH);

            int N = 100_000;
            var rnd = new Random();

            var sw = new Stopwatch();
            using var db = new LiteEngine(PATH);

            var docs = Enumerable.Range(1, N).Select(x => new BsonDocument
            {
                ["name"] = "John " + Guid.NewGuid().ToString(),
                ["age"] = rnd.Next(0, 99)
            }).ToArray();

            var docs2 = Enumerable.Range(1, N).Select(x => new BsonDocument
            {
                ["name"] = "John " + Guid.NewGuid().ToString(),
                ["age"] = rnd.Next(0, 99)
            }).ToArray();


            sw.Start();

            db.Insert("col1", docs, BsonAutoId.Int32);

            sw.Restart();
            LiteEngine.COMPARE.Reset();
            LiteEngine.GET_NODE_CACHE.Reset();
            LiteEngine.GET_NODE_DISK.Reset();


            db.Insert("col1", docs2, BsonAutoId.Int32);
            //db.EnsureIndex("col1", "idx_name", "$.name", false);
            //db.EnsureIndex("col1", "idx_age", "$.age", false);

            sw.Stop();
            Console.WriteLine("Done...");


            string print(Counter s) => s.Stopwatch.ElapsedMilliseconds + "  (" + Math.Round((decimal)((decimal)s.Stopwatch.ElapsedMilliseconds / (decimal)sw.ElapsedMilliseconds) * (decimal)100, 2) + "%) - " + s.Count.ToString("n0");

            Console.WriteLine("Total Inserted: " + N.ToString("n0"));
            Console.WriteLine("Elapsed (ms): " + sw.ElapsedMilliseconds);
            Console.WriteLine("GET_NODE_DISK (ms): " + print(LiteEngine.GET_NODE_DISK));
            Console.WriteLine("GET_NODE_CACHE (ms): " + print(LiteEngine.GET_NODE_CACHE));
            Console.WriteLine("COMPARE (ms): " + print(LiteEngine.COMPARE));

        }
    }
}
