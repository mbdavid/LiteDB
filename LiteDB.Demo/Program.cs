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
        static void Main(string[] args)
        {
            var timer = new Stopwatch();
            ITest test = new LiteDB_Paging();
            //ITest test = new SQLite_Paging();

            Console.WriteLine("Testing: {0}", test.GetType().Name);

            test.Init();

            Console.WriteLine("Populating 100.000 documents...");

            timer.Start();
            test.Populate(ReadDocuments());
            timer.Stop();

            Console.WriteLine("Done in {0}ms", timer.ElapsedMilliseconds);

            timer.Restart();
            var counter = test.Count();
            timer.Stop();
            
            Console.WriteLine("Result query counter: {0} ({1}ms)", counter, timer.ElapsedMilliseconds);
            
            var input = "0";
            
            while (input != "")
            {
                var skip = Convert.ToInt32(input);
                var limit = 10;
            
                timer.Restart();
                var result = test.Fetch(skip, limit);
                timer.Stop();
            
                foreach(var doc in result)
                {
                    Console.WriteLine(
                        doc["_id"].AsString.PadRight(6) + " - " +
                        doc["name"].AsString.PadRight(30) + "  -> " +
                        doc["age"].AsInt32);
                }
            
                Console.Write("\n({0}ms) => Enter skip index: ", timer.ElapsedMilliseconds);
                input = Console.ReadLine();
            }
            
            Console.WriteLine("End");
            Console.ReadKey();
        }

        static IEnumerable<BsonDocument> ReadDocuments()
        {
            using (var s = File.OpenRead(@"datagen.txt"))
            {
                var r = new StreamReader(s);

                while(!r.EndOfStream)
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
                        };
                    }
                }
            }
        }
    }
}