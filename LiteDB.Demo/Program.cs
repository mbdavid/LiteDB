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

                var r = db.Query("col1")
                    .Where("age  = @0", 63)
                    .Where("name  = @0", "Iliana Wilson")
                    .Where("UPPER(name) = @0", "ILIANA WILSON")
                    .Where("_id  = @0", 199)
                    .FirstOrDefault();

                // {"_id":199,"name":"Iliana Wilson","age":63,"email":"Piper@molestie.org","lorem":"-"}

                Console.WriteLine();
                Console.WriteLine(r?.ToString() ?? "<null>");

                ;
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
