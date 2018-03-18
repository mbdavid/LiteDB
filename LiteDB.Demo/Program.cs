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
            //TestChunk.Run();

            File.Delete(datafile);

            using (var db = new LiteEngine(new ConnectionString { Filename = datafile, Timeout = TimeSpan.FromSeconds(2) }))
            {
                //db.Insert("col1", ReadDocuments(1, 10, false, false), BsonAutoId.Int32);
                //db.EnsureIndex("col1", "age", BsonExpression.Create("age"), false);
                //
                //using (var t = db.BeginTrans())
                //{
                //    var r = db.Query("col1")
                //        //.Where("age between 14 and 20")
                //        //.GroupBy("_id > 0")
                //        //.Select("{ s: count($), total: count($) }")
                //        //.Select("{_id,data:DATE(), name,age}")
                //        .ToEnumerable();
                //
                //    foreach(var x in r)
                //    {
                //        x["data"] = DateTime.Now;
                //    
                //        db.Update("col1", new BsonDocument[] { x });
                //    }
                //    
                //    Console.WriteLine("LENGTH: {0}", r.Count());
                //
                //    db.Analyze(new string[] { "col1" });
                //
                //    t.Commit();
                //}
                //
                //db.Checkpoint();
                //
                //var r0 = db.Query("col1").ToList();
                //Console.WriteLine(JsonSerializer.Serialize(new BsonArray(r0), true));

                db.Insert("endereco", new BsonDocument[] { new BsonDocument { ["_id"] = 1, ["rua"] = "Ipiranga" } }, BsonAutoId.ObjectId);
                db.Insert("endereco", new BsonDocument[] { new BsonDocument { ["_id"] = 2, ["rua"] = "Protasio" } }, BsonAutoId.ObjectId);

                db.Insert("cliente", new BsonDocument[] { new BsonDocument { ["_id"] = 1, ["nome"] = "John", ["endereco"] = BsonValue.DbRef(1, "endereco") } }, BsonAutoId.ObjectId);
                db.Insert("cliente", new BsonDocument[] { new BsonDocument { ["_id"] = 2, ["nome"] = "Carlos", ["endereco"] = BsonValue.DbRef(1, "endereco") } }, BsonAutoId.ObjectId);
                db.Insert("cliente", new BsonDocument[] { new BsonDocument { ["_id"] = 3, ["nome"] = "Maria", ["endereco"] = BsonValue.DbRef(3, "endereco") } }, BsonAutoId.ObjectId);

                //db.Checkpoint();

                //var r = db.Query("cliente")
                //    .ToArray();
                //;


                var r = db.Query("$dump")
                    //.Where("pageID = 0")
                    .GroupBy("pageType")
                    .Select("{pageType,tot: COUNT($)}")
                    .OrderBy("tot", -1)
                    .ToList();

                Console.WriteLine(JsonSerializer.Serialize(new BsonArray(r), true));



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
