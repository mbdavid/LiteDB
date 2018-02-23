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
                db.Insert("col1", ReadDocuments(1, 1000));

                db.EnsureIndex("col1", "idx_age", "age");
                db.EnsureIndex("col1", "idx_name", "name");
                db.EnsureIndex("col1", "idx_name_upper", "UPPER(name)");
                //db.EnsureIndex("col1", "idx_email", "email");

                using (var t = db.BeginTrans())
                {
                    var r = db.Query("col1", t)
                        .Where("_id = items([@0,@1])", 63, 64)
                        //.Where("age = @0", 63)
                        //.Where("name like @0", "Il%")
                        //.Where("UPPER(name) = @0", "ILIANA WILSON")
                        //.Where("email = @0", "Piper@molestie.org")
                        //.Where("_id  = ITEMS(@0)", new BsonArray(new BsonValue[] { -5, 199, 200, 99999 }))
                        //.OrderBy("upper( name)", Query.Ascending)
                        .Limit(5)
                        .ToArray();

                    // {"_id":199,"name":"Iliana Wilson","age":63,"email":"Piper@molestie.org","lorem":"-"}

                    Console.WriteLine();
                    Console.WriteLine(JsonSerializer.Serialize(new BsonArray(r.Select(x => x.AsDocument)), true));

                    ;

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
                            ["lorem"] = bigDoc ? row[3] : "-"
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
