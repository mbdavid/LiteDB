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

            using (var l = new Logger(@"filename=C:\Git\Temp\stress\eventLog.db; mode=shared"))
            using (var e = new ExampleStressTest(@"C:\Git\Temp\stress\example-1.db", l))
            {
                e.Run(TimeSpan.FromMinutes(2));
            }

            Console.WriteLine("Stress test finish");

            Console.ReadKey();
        }
    }


}
