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
            Console.WriteLine("Stress Test");
            Console.WriteLine("===========");
            Console.Write("Enter time (minutes): ");
            var timer = Console.ReadLine();

            using (var e = new ExampleStressTest(@"C:\Git\Temp\stress\example-1.db"))
            {
                e.Synced = true;

                e.Run(TimeSpan.FromMinutes(string.IsNullOrEmpty(timer) ? .5 : Convert.ToDouble(timer)));
            }

            Console.WriteLine("End");
            Console.ReadKey();
        }
    }


}
