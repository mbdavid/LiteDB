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
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    class Paging
    {
        static string filename = Path.Combine(Path.GetTempPath(), "file_paging.db");

        public static void StartTest()
        {
            File.Delete(filename);

            using (var db = new LiteDatabase(filename))
            {
                var people = db.GetCollection<Person>();

                Console.WriteLine("Populating...");

                // pouplate collection
                people.InsertBulk(Populate(50000));

                // create indexes
                people.EnsureIndex(x => x.Name);
                people.EnsureIndex(x => x.Age);

                // query by age
                var query = Query.EQ("Age", 22);

                // show count result
                Console.WriteLine("Result count: " + people.Count(query));

                var page = "0";

                while(page != "")
                {
                    var timer = new Stopwatch();

                    timer.Start();

                    var result = people.FindPaged(db, query, "$.Name", Query.Ascending, Convert.ToInt32(page), 10);

                    timer.Stop();

                    Console.WriteLine("\n\nPage index: " + page + " (ms: " + timer.ElapsedMilliseconds + ")");

                    foreach (var doc in result)
                    {
                        Console.WriteLine(doc.Id.ToString().PadRight(6) + " - " + doc.Name + "  -> " + doc.Age);
                    }

                    Console.Write("\nEnter new page index: ");
                    page = Console.ReadLine();
                }
            }
        }

        static IEnumerable<Person> Populate(int count)
        {
            var rnd = new Random();

            for(var i = 1; i <= count; i++)
            {
                yield return new Person
                {
                    Id = i,
                    Name = Guid.NewGuid().ToString("d"),
                    Age = rnd.Next(18, 40)
                };
            }
        }
    }

    public static class PagingExtensions
    {
        public static List<T> FindPaged<T>(this LiteCollection<T> col, LiteDatabase db, Query query, string orderByExpr, int order, int pageIndex, int pageSize)
        {
            var tmp = "tmp_" + Guid.NewGuid().ToString().Substring(0, 5);
            var engine = db.Engine;

            // insert unsorted result inside a temp collection
            engine.InsertBulk(tmp, engine.Find(col.Name, query));

            // create an index to order by this result
            engine.EnsureIndex(tmp, "orderBy", orderByExpr);

            var skip = pageIndex * pageSize;

            // now, get all documents in temp orderBy expr with skip/limit 
            var sorted = engine.Find(tmp, Query.All("orderBy", order), skip, pageSize);

            // convert docs to T entity
            var list = new List<T>(sorted.Select(x => db.Mapper.ToObject<T>(x)));

            // drop temp collection
            engine.DropCollection(tmp);

            return list;
        }
    }
}