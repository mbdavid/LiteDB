using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("LITE DB v5");
            Console.WriteLine("===========================================================");

            File.Delete(@"d:\test-1m.db");
            File.Delete(@"d:\test-1m-log.db");

            var file = @"d:\test-1m.db";
            var sw = new Stopwatch();

            using (var db = new LiteDatabase(file))
            {
                var col = db.GetCollection<Event>("event");

                var tmp = new List<Event>();

                for (var i = 0; i < 1_000_000; i++)
                {
                    tmp.Add(new Event()
                    {
                        Data = "the quick brown fox jumps over the lazy dog",
                        DateTime = DateTime.Now
                    });
                }

                // -----------------

                sw.Restart();

                col.Insert(tmp);

                Console.WriteLine("Insert: " + sw.Elapsed);

                // -----------------

                sw.Restart();

                db.Checkpoint();

                Console.WriteLine("Checkpoint: " + sw.Elapsed);

                // -----------------

                sw.Restart();

                col.EnsureIndex(x => x.DateTime);

                Console.WriteLine("EnsureIndex: " + sw.Elapsed);
            }

            Console.WriteLine(" ===========================================================");
            Console.WriteLine("End");
            Console.ReadKey();
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Name { get; set; }
        public List<User> Children { get; set; }
    }

    public class Event
    {
        public Guid Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Data { get; set; }
    }


}
