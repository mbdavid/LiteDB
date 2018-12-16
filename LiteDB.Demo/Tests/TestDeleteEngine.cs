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
    public class TestDeleteEngine
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
                CheckpointOnShutdown = false
            };

            sw.Start();

            using (var db = new LiteEngine(settings))
            {
                db.EnsureIndex("col1", "idx_1", "rnd", false);

                GetDocs(1, 4).ToList().ForEach(d => db.Insert("col1", new[] { d }, BsonAutoId.Int32));

                Console.WriteLine(db.CheckIntegrity());

                db.Delete("col1", new BsonValue[] { 1, 2, 3, 4 });

                db.Checkpoint(CheckpointMode.Full);

                Console.WriteLine(db.CheckIntegrity());
            }

            sw.Stop();
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
