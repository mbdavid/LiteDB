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
            File.Delete(datafile);

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile, Timeout = TimeSpan.FromSeconds(2) }))
            {
                db.Insert("col1", ReadDocuments(1, 50000, false, true));
                db.EnsureIndex("col", "age", "age");

                using (var t = db.BeginTrans())
                {
                    var r = db.Query("col1", t)
                        //.Where("age between 14 and 20")
                        .GroupBy("_id > 0")
                        .Select("{ s: count($), total: count($) }")
                        .ToList();


                    Console.WriteLine("LENGTH: {0}", r.Count);
                    Console.WriteLine(JsonSerializer.Serialize(new BsonArray(r), true));
                }
            }

            Console.WriteLine("End");
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
