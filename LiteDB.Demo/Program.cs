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
    class Program
    {
        private static string datafile = @"c:\temp\app.db";
        private static string walfile = @"c:\temp\app-wal.db";


        static void Main(string[] args)
        {
            File.Delete(datafile);
            File.Delete(walfile);

            var sw = new Stopwatch();
            var log = new Logger(Logger.LOCK, (s) => Console.WriteLine("> " + s));

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile, Log = log }))
            {
                var ts = new List<Task>();
                sw.Start();

                db.EnsureIndex("col1", "age", new BsonExpression("$.age"), false);
                db.EnsureIndex("col2", "age", new BsonExpression("$.age"), false);

                Console.Clear();

                try
                {
                    db.Insert("col1", ReadDocuments(200, true), BsonAutoId.ObjectId);
                }
                catch
                {
                }
                db.Insert("col1", ReadDocuments(400, true), BsonAutoId.ObjectId);

                //ts.Add(Task.Run(() =>
                //{
                //    db.Insert("col1", ReadDocuments(), BsonAutoId.ObjectId);
                //}));
                //ts.Add(Task.Run(() =>
                //{
                //    db.Insert("col2", ReadDocuments(), BsonAutoId.ObjectId);
                //}));

                Task.WaitAll(ts.ToArray());

                // testing find in col2
                var d = db.Find("col2", Query.EQ("_id", 3)).FirstOrDefault();
                Console.WriteLine(d?.AsDocument["name"].AsString);

                Console.WriteLine("Total (b/WAL): " + sw.ElapsedMilliseconds);

            }

            sw.Stop();
            Console.WriteLine("Total (a/WAL): " + sw.ElapsedMilliseconds);


            using (var db = new LiteEngine(datafile))
            {
                //var s = db.Info();
                //Console.WriteLine(JsonSerializer.Serialize(db.Info(), true));

                // test find in col1
                var d = db.Find("col1", Query.EQ("_id", 3)).FirstOrDefault();

                Console.WriteLine(d ?.AsDocument["name"].AsString);
            }


            Console.WriteLine("End");
            Console.ReadKey();
        }

        static IEnumerable<BsonDocument> ReadDocuments(int counter = 100, bool duplicate = false)
        {
            using (var s = File.OpenRead(@"datagen.txt"))
            {
                var r = new StreamReader(s);

                while(!r.EndOfStream && --counter > 0)
                {
                    var line = r.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        var row = line.Split(',');

                        yield return new BsonDocument
                        {
                            ["_id"] = Convert.ToInt32(row[0]),
                            ["name"] = row[1],
                            ["age"] = Convert.ToInt32(row[2])
                            //["lorem"] = "".PadLeft(9000, '-')
                        };
                    }
                }

                // simulate error
                if (duplicate)
                {
                    yield return new BsonDocument { ["_id"] = 1 };
                }
            }
        }
    }
}