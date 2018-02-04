using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class Program
    {
        private static string datafile = @"d:\git\temp\app-5.db";

        static void Main(string[] args)
        {
            while (true) Run();

            Console.WriteLine("End");
            Console.ReadKey();
        }

        static void Run()
        {
            File.Delete(datafile);

            var sw = new Stopwatch();

            long tOpen, tCreate, tInsert, tCheckpoint, tClose = 0;

            sw.Start();

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile, Timeout = TimeSpan.FromSeconds(2) }))
            {
                tOpen = sw.ElapsedMilliseconds;

                using (var t = db.BeginTrans())
                {
                    db.CreateCollection("col1", t);
                    db.CreateCollection("col2", t);
                    db.CreateCollection("col3", t);
                    db.CreateCollection("col4", t);
                    t.Commit();
                }

                tCreate = sw.ElapsedMilliseconds;

                //db.EnsureIndex("col1", "age", "$.age");
                //db.EnsureIndex("col2", "age", "$.age");
                //db.EnsureIndex("col3", "age", "$.age");
                //db.EnsureIndex("col4", "age", "$.age");

                var t1 = db.InsertAsync("col1", ReadDocuments(1, 50000, false, true));
                var t2 = db.InsertAsync("col2", ReadDocuments(1, 50000, false, true));
                var t3 = db.InsertAsync("col3", ReadDocuments(1, 50000, false, true));
                var t4 = db.InsertAsync("col4", ReadDocuments(1, 50000, false, true));
                Task.WaitAll(new Task[] { t1, t2, t3, t4 });

                //db.Insert("col1", ReadDocuments(1, 50000, false, true));
                //db.Insert("col2", ReadDocuments(1, 50000, false, true));
                //db.Insert("col3", ReadDocuments(1, 50000, false, true));
                //db.Insert("col4", ReadDocuments(1, 50000, false, true));


                tInsert = sw.ElapsedMilliseconds;

                db.Checkpoint();

                tCheckpoint = sw.ElapsedMilliseconds;

            }

            tClose = sw.ElapsedMilliseconds;

            sw.Stop();

            Console.WriteLine("Time to open database: " + tOpen);
            Console.WriteLine("Finish create collections: " + tCreate);
            Console.WriteLine("Finish insert: " + tInsert);
            Console.WriteLine("Finish checkpoint: " + tCheckpoint);
            Console.WriteLine("Finish close database: " + tClose);

        }


        static IEnumerable<BsonDocument> ReadDocuments(int start = 1, int end = 100000, bool duplicate = false, bool bigDoc = false)
        {
            var count = start;

            using (var s = File.OpenRead(@"c:\git\temp\datagen.txt"))
            {
                var r = new StreamReader(s);

                while (!r.EndOfStream && count <= end)
                {
                    var line = r.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        var row = line.Split(',');

                        yield return new BsonDocument
                        {
                            ["_id"] = count++,
                            ["name"] = row[0],
                            ["age"] = Convert.ToInt32(row[1]),
                            ["email"] = row[2],
                            ["lorem"] = bigDoc ? row[3] : "-"
                        };
                    }
                }

                // simulate error
                if (duplicate)
                {
                    yield return new BsonDocument { ["_id"] = start };
                }
            }
        }
    }
}
