using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
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

            Console.Write("Enter test duration (minutes): ");

            var input = Console.ReadLine();
            var timer = TimeSpan.FromMinutes(string.IsNullOrEmpty(input) ? 1 : Convert.ToDouble(input));

            using (var test = new ExampleStressTest(@"example.db"))
            {
                test.Run(timer);
            }

            Console.WriteLine("End");
            Console.ReadKey();
        }
    }
}
