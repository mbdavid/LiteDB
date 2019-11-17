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

            using (var l = new Logger(@"filename=d:\stress\eventLog.db; mode=shared"))
            using (var e = new ExampleStressTest(@"d:\stress\example.db", l))
            {
                e.Run(TimeSpan.FromMinutes(.5));
            }

            Console.WriteLine("Stress test finish");

            Console.ReadKey();
        }
    }


}
