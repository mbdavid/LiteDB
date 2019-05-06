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

            var rnd = new Random();

            var list = Enumerable.Range(0, 10_000).Select(x => new KeyValuePair<BsonValue, PageAddress>(rnd.Next(0, 1000), PageAddress.Empty));
            //var list = Enumerable.Range(0, 1_000).Select(x => new KeyValuePair<BsonValue, PageAddress>(
            //    (Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n"))
            //    .Substring(0, rnd.Next(5, 64)), PageAddress.Empty));

            var sw = new Stopwatch();
            sw.Start();
            //
            //list.OrderBy(x => x.Key).Count();
            //
            //sw.Stop();
            //Console.WriteLine("Elapsed (linq): " + sw.ElapsedMilliseconds + " ms");
            //sw.Restart();

            // using (var s = new MergeSortService(100 * 8192, false))
            // {
            //     //var result = s.Sort(list, Query.Ascending).Count();
            // 
            //     s.Sort(list, Query.Descending).ToList().ForEach(x => Console.Write(x.Key.AsInt32 + ";"));
            // }

            sw.Stop();
            Console.WriteLine("\nElapsed (merge): " + sw.ElapsedMilliseconds + " ms");

            

            Console.WriteLine("===========================================================");
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



}
