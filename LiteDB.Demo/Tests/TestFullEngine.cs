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
    public class TestFullEngine
    {
        static Random RND = new Random();
        static string PATH = @"D:\insert-vf.db";
        static string PATH_LOG = @"D:\insert-vf-log.db";

        public static void Run(Stopwatch sw)
        {
            File.Delete(PATH);
            File.Delete(PATH_LOG);

            var settings = new EngineSettings
            {
                Filename = PATH
            };

            var query = new QueryDefinition();
            var expr = BsonExpression.Create("_id = @p0");
            query.Where.Add(expr);

            sw.Start();

            using (var db = new LiteEngine(settings))
            {
                Console.WriteLine("Insert...");

                var t0 = Task.Run(() => db.Insert("col1", GetDocs(1, 100000), BsonAutoId.Int32));
                //var t1 = Task.Run(() => db.Insert("col2", GetDocs(1, 10000), BsonAutoId.ObjectId));
                //var t2 = Task.Run(() => db.Insert("col3", GetDocs(1, 1000), BsonAutoId.Guid));

                Task.WaitAll(t0/*, t1, t2*/);
            }

            sw.Stop();

            //using (var db = new LiteEngine(settings))
            //{
            //    db.CheckIntegrity();
            //}
        }

        static IEnumerable<BsonDocument> GetDocs(int start, int count)
        {
            var end = start + count;

            for (var i = start; i < end; i++)
            {
                yield return new BsonDocument
                {
                    ["_id"] = i, // Guid.NewGuid(),
                    ["rnd"] = Guid.NewGuid(),
                    ["name"] = "NoSQL Database",
                    ["birthday"] = new DateTime(1977, 10, 30),
                    ["phones"] = new BsonArray { "000000", "12345678" },
                    ["bytes"] = new byte[750],
                    ["active"] = true
                };
            }
        }
    }
}
