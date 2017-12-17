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
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string[] Phones { get; set; }
        public bool IsActive { get; set; }
    }

    class ProgramCustomer
    {
        static void Main(string[] args)
        {
            var timer = new Stopwatch();

            File.Delete(@"MyData.db");

            timer.Start();

            using (var db = new LiteDatabase(@"filename=MyData.db;journal=false"))
            {
                var customers = db.GetCollection<Customer>("customers");

                customers.Insert(GetCustomers());
            }

            timer.Stop();

            Console.WriteLine("Insert IEnumerable done in {0}ms", timer.ElapsedMilliseconds);

            File.Delete(@"MyData.db");

            timer.Start();

            using (var db = new LiteDatabase(@"filename=MyData.db;journal=false"))
            {
                var customers = db.GetCollection<Customer>("customers");

                foreach(var item in GetCustomers())
                {
                    customers.Insert(item);
                }
            }

            timer.Stop();

            Console.WriteLine("Insert one-by-one done in {0}ms", timer.ElapsedMilliseconds);


            Console.WriteLine("End");
            Console.ReadKey();
        }

        public static IEnumerable<Customer> GetCustomers()
        {
            for (int i = 0; i < 10000; i++)
            {
                yield return new Customer
                {
                    Name = "John Doe",
                    Phones = new string[] { "8000-0000", "9000-0000" },
                    IsActive = true
                };
            }
        }
    }
}