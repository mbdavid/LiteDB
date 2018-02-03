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

            var timer = new Stopwatch();

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile }))
            {
                db.EnsureIndex("col1", "age", "$.age");
                db.Insert("col1", ReadDocuments(1, 100000, false, false));

                var input = "0";

                // Console.WriteLine("Count: " + db.Find("col1", new Query { Index = Index.EQ("age", 22), KeyOnly = true }).Count());

                while (input != "")
                {
                    var offset = Convert.ToInt32(input);
                    var limit = 10;

                    timer.Restart();

                    using (var t = db.BeginTrans())
                    {
                        var query = new Query
                        {
                            Index = Index.EQ("age", 22),
                            Offset = offset,
                            Limit = limit,
                            OrderBy = new BsonExpression("$.name")
                        };

                        var result = db.Find("col1", query, t);

                        foreach (var doc in result)
                        {
                            Console.WriteLine(
                                doc["_id"].AsString.PadRight(6) + " - " +
                                doc["name"].AsString.PadRight(30) + "  -> " +
                                doc["age"].AsInt32);
                        }
                    }

                    timer.Stop();

                    Console.Write("\n({0}ms) => Enter skip index: ", timer.ElapsedMilliseconds);

                    input = Console.ReadLine();
                    //input = "";
                }
            }

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