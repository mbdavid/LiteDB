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
        private static string datafile = @"c:\temp\app.db";
        private static string walfile = @"c:\temp\app-wal.db";


        static void Main(string[] args)
        {
            File.Delete(datafile);
            File.Delete(walfile);

            var sb = new StringBuilder();

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

                //sb.AppendLine("After Erro 200\n=========================");
                //sb.AppendLine("DataFile:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDataFile()), true));
                //sb.AppendLine("\nWalFile:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpWalFile()), true));
                //sb.AppendLine("\nDatabase:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDatabase()), true));

                db.Insert("col1", ReadDocuments(400, false), BsonAutoId.ObjectId);

                //sb.AppendLine("After Insert 400\n=========================");
                //sb.AppendLine("DataFile:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDataFile()), true));
                //sb.AppendLine("\nWalFile:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpWalFile()), true));
                //sb.AppendLine("\nDatabase:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDatabase()), true));

                ts.Add(Task.Run(() =>
                {
                    db.Insert("col3", ReadDocuments(), BsonAutoId.ObjectId);
                }));
                ts.Add(Task.Run(() =>
                {
                    db.Insert("col4", ReadDocuments(), BsonAutoId.ObjectId);
                }));

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
                sb.AppendLine("After Checkpoint\n=========================");
                sb.AppendLine("DataFile:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDataFile()), true));
                sb.AppendLine("\nWalFile:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpWalFile()), true));
                sb.AppendLine("\nDatabase:\n" + JsonSerializer.Serialize(new BsonArray(db.DumpDatabase()), true));

                //var s = db.Info();
                //Console.WriteLine(JsonSerializer.Serialize(db.Info(), true));

                // test find in col1
                var d = db.Find("col1", Query.EQ("_id", 3)).FirstOrDefault();

                Console.WriteLine(d ?.AsDocument["name"].AsString);
            }


            var j = sb.ToString();

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