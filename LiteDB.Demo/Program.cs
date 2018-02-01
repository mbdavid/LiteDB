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
    class Program
    {
        private static string datafile = @"c:\temp\app-5.db";

        static void Main(string[] args)
        {
            File.Delete(datafile);

            var sb = new StringBuilder();
            var sw = new Stopwatch();

            sw.Start();

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile }))
            {
                //using (var t = db.BeginTrans())
                //{
                //    db.CreateCollection("col1", t);
                //    db.CreateCollection("col2", t);
                //    db.CreateCollection("col3", t);
                //    db.CreateCollection("col4", t);
                //
                //    t.Commit();
                //
                //    sb.AppendLine("Before:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDatafile()), true));
                //}

                var t0 = db.Insert("col1", ReadDocuments(1, 200, false, true));
                //var t1 = db.InsertAsync("col2", ReadDocuments(1, 50000, false, true));
                //var t2 = db.InsertAsync("col3", ReadDocuments(1, 50000, false, true));
                //var t3 = db.InsertAsync("col4", ReadDocuments(1, 50000, false, true));

                //db.DropCollection("col1");

                //Task.WaitAll(new Task[] { t0, t1, t2, t3, t3 });

                //var r0 = db.FindById("col2", 2000);

                //Console.WriteLine(JsonSerializer.Serialize(r0, true));

                sb.AppendLine("Before:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDatafile()), true));
                Console.WriteLine("Total ms: " + sw.ElapsedMilliseconds);

                db.Checkpoint();

                sb.AppendLine("After:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDatafile()), true));


            }

            sw.Stop();

            Console.WriteLine("Total ms: " + sw.ElapsedMilliseconds);

            var debug = sb.ToString();

            //Console.WriteLine(debug);

            Console.WriteLine("End");
            Console.ReadKey();
        }

        static IEnumerable<BsonDocument> ReadDocuments(int start = 1, int end = 100000, bool duplicate = false, bool bigDoc = false)
        {
            var count = start;

            using (var s = File.OpenRead(@"datagen.txt"))
            {
                var r = new StreamReader(s);

                while(!r.EndOfStream && count <= end)
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