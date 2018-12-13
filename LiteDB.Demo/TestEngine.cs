using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class TestEngine
    {
        static Random RND = new Random();
        static string PATH = @"D:\memory-file.db";
        static string PATH_LOG = @"D:\memory-file-log.db";

        public static void Run(Stopwatch sw)
        {
            File.Delete(PATH);
            File.Delete(PATH_LOG);

            var settings = new EngineSettings
            {
                Filename = PATH,
                CheckpointOnShutdown = true
            };

            sw.Start();

            using (var db = new LiteEngine(settings))
            {
                //db.EnsureIndex("col1", "idx_1", "rnd", false);
                //db.EnsureIndex("col2", "idx_1", "rnd", false);

                var ta = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Begin: col1");
                    GetDocs(1, 500).ToList().ForEach(d => db.Insert("col1", new[] { d }, BsonAutoId.Int32));
                    //db.Insert("col1", GetDocs(1, 10000), BsonAutoId.Int32);
                    Console.WriteLine("End: col1");
                });

                var tb = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Begin: col2");
                    //GetDocs(1, 100).ToList().ForEach(d => db.Insert("col2", new[] { d }, BsonAutoId.Int32));
                    db.Insert("col2", GetDocs(1, 50000), BsonAutoId.Int32);
                    Console.WriteLine("End: col2");
                });

                Task.WaitAll(ta, tb);

                Console.WriteLine(db.CheckIntegrity());

                sw.Stop();
            }

            using (var db = new LiteEngine(settings))
            {

                db.FindAll("col1").Count();
                db.FindAll("col2").Count();

                // wait writer thread finish
                // Thread.Sleep(3000);


                db.Dispose();

                Debug.Assert(db.PagesInUse == 0);

            }

        }

        static IEnumerable<BsonDocument> GetDocs(int start, int count)
        {
            var end = start + count;

            for (var i = start; i < end; i++)
            {
                yield return new BsonDocument
                {
                    ["_id"] = i, // Guid.NewGuid(),
                    ["rnd"] = Guid.NewGuid().ToString(),
                    ["name"] = "NoSQL Database",
                    ["birthday"] = new DateTime(1977, 10, 30),
                    ["phones"] = new BsonArray { "000000", "12345678" },
                    ["bytes"] = new byte[RND.Next(30, 1500)],
                    ["active"] = true
                };
            }
        }
    }
}
