using LiteDB;
using LiteDB.Engine;
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
        private static string walfile = @"c:\git\temp\app-5-wal.db";

        static void Main(string[] args)
        {
            var settings = new EngineSettings
            {
                Filename = datafile,
                Timeout = TimeSpan.FromSeconds(2)
            };

            File.Delete(datafile);
            File.Delete(walfile);

            using (var db = new LiteEngine(settings))
            {
                Task.Factory.StartNew(() => db.Insert("col1", ReadDocuments(1, 100), BsonAutoId.Int32));

                Task.Factory.StartNew(() => db.Insert("col2", ReadDocuments(1, 5000), BsonAutoId.Int32));

                Task.Delay(150).Wait();
            }

            Console.WriteLine("Engine Disposed()");


            using (var db = new LiteEngine(settings))
            {
                var c1 = db.Query("col1").ToEnumerable().Count();
                var c2 = db.Query("col2").ToEnumerable().Count();

                Console.WriteLine("Count col1: " + c1);
                Console.WriteLine("Count col2: " + c2);
            }

            Console.ReadKey();
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
                            ["_id"] = count,
                            ["idx"] = count,
                            ["name"] = row[0],
                            ["age"] = Convert.ToInt32(row[1]),
                            ["email"] = row[2],
                            ["lorem"] = bigDoc ? row[3].PadLeft(1000) : "-"
                        };

                        count++;
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
