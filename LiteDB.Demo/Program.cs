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
    class Program
    {
        static void Main(string[] args)
        {
            //Concurrency.StartTest();
            Paging.StartTest();

            Console.WriteLine("End");
            Console.ReadKey();
        }
    }
}