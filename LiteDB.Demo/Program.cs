using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class Program
    {
        private static string datafile = @"c:\git\temp\app-5.db";

        static void Main(string[] args)
        {
            var e = BsonExpression.Create("arr[@p1]");

            e.Parameters["p1"] = 0;
            

            var r = e.Execute(new BsonDocument { ["a"] = 1 }).First();

            //var r = e.Execute(new BsonDocument()).First();

            ;


            /*
            File.Delete(datafile);

            var total = 0d;
            var counter = 0d;

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile, Timeout = TimeSpan.FromSeconds(2) }))
            {
                while (true)
                {
                    total += Run(db, (int)counter);
                    counter++;
                    Console.WriteLine("=> Average: " + (total / counter).ToString("0") + " (" + counter + ")");
                }
            }
            */

            Console.WriteLine("End");
            Console.ReadKey();
        }

        static long Run(LiteEngine db, int count)
        {
            var sw = new Stopwatch();

            var COL1 = "col1_" + count;
            var COL2 = "col2_" + count;
            var COL3 = "col3_" + count;
            var COL4 = "col4_" + count;

            sw.Start();

            //using (var t = db.BeginTrans())
            //{
            //    db.CreateCollection(COL1, t);
            //    db.CreateCollection(COL2, t);
            //    db.CreateCollection(COL3, t);
            //    db.CreateCollection(COL4, t);
            //    t.Commit();
            //}

            //Console.WriteLine("Time to create collections: " + sw.ElapsedMilliseconds);

            var ti1 = db.InsertAsync(COL1, ReadDocuments(1, 50000, false, true));
            //var ti2 = db.InsertAsync(COL2, ReadDocuments(1, 50000, false, true));
            //var ti3 = db.InsertAsync(COL3, ReadDocuments(1, 50000, false, true));
            //var ti4 = db.InsertAsync(COL4, ReadDocuments(1, 50000, false, true));
            Task.WaitAll(new Task[] { ti1/*, ti2, ti3, ti4*/ });

            //db.EnsureIndex(COL1, "age", "$.age");
            //db.EnsureIndex(COL1, "email", "$.email");


            //Console.WriteLine("Time to insert documents: " + sw.ElapsedMilliseconds);
            // 
            // var tf1 = db.FindAllAsync(COL1);
            // var tf2 = db.FindAllAsync(COL2);
            // var tf3 = db.FindAllAsync(COL3);
            // var tf4 = db.FindAllAsync(COL4);
            // Task.WaitAll(new Task[] { tf1, tf2, tf3, tf4 });
            // 
            // Console.WriteLine("Time to find documents: " + sw.ElapsedMilliseconds);
            // 
            // // drop all collections
            // using (var t = db.BeginTrans())
            // {
            //     db.DropCollection(COL1, t);
            //     db.DropCollection(COL2, t);
            //     db.DropCollection(COL3, t);
            //     db.DropCollection(COL4, t);
            //     t.Commit();
            // }
            // 
            // Console.WriteLine("Time to drop collections: " + sw.ElapsedMilliseconds);
            // 
            // db.Checkpoint();
            // 
            // Console.WriteLine("Time to checkpoint: " + sw.ElapsedMilliseconds);
            // 
            // sw.Stop();
            // db.WaitAsyncWrite();


            // db.Analyze();
            // 
            // db.Checkpoint();
            // db.WaitAsyncWrite();
            // 
            // var total = db.Find(COL1, new Query { Index = Index.All() });
            // 
            // var d = JsonSerializer.Serialize(new BsonArray(db.DumpDatafile()), true);

            return sw.ElapsedMilliseconds;
        }


        static IEnumerable<BsonDocument> ReadDocuments(int start = 1, int end = 50000, bool duplicate = false, bool bigDoc = false)
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
