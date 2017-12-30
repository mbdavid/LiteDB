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
            var datafile = @"c:\temp\app.db";
            var walfile = @"c:\temp\app-wal.db";

            File.Delete(datafile);
            File.Delete(walfile);


            using (var db = new LiteEngine(datafile))
            {
                db.Insert("col", new BsonDocument[] { new BsonDocument { ["_id"] = 1, ["name"] = "Mauricio David 2016" } }, BsonAutoId.ObjectId);
                db.Insert("col", new BsonDocument[] { new BsonDocument { ["_id"] = 2, ["name"] = "Mauricio David 2017" } }, BsonAutoId.ObjectId);
                db.Insert("col", new BsonDocument[] { new BsonDocument { ["_id"] = 3, ["name"] = "Mauricio David 2018" } }, BsonAutoId.ObjectId);

                var d = db.Find("col", Query.EQ("_id", 1)).FirstOrDefault();

                Console.WriteLine(d.ToString());
            }

            using (var db = new LiteEngine(datafile))
            {
                var d = db.Find("col", Query.EQ("_id", 3)).FirstOrDefault();

                Console.WriteLine(d.ToString());
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