using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Perf
{
    class Program
    {
        static string filename = "file_demo.db";
        static TimeSpan timeout = new TimeSpan(0, 10, 0);
        static Logger log = new Logger(Logger.LOCK, (s) => Console.WriteLine(s));

        static void Main(string[] args)
        {
            ExecuteTest("Process", TestProcess, 200);
            //ExecuteTest("Thread", TestThread, 5000);

            Console.WriteLine("End");
            Console.ReadKey();
        }

        static LiteEngine InitDB()
        {
            var disk = new FileDiskService(filename, true);
            return new LiteEngine(disk, null, timeout, 5000, log);
        }

        static void ExecuteTest(string name, Action<int> test, int n)
        {
            Console.WriteLine("{0} (N = {1})", name, n);

            // delete datafile before starts
            File.Delete(filename);

            // create empty database and collection with index in name
            using (var db = new LiteEngine(filename))
            {
                db.EnsureIndex("collection", "name");
            }

            var s = new Stopwatch();
            s.Start();

            // execute test
            test(n);

            s.Stop();

            Console.WriteLine("Time Elapsed (ms): " + s.ElapsedMilliseconds);
            Console.WriteLine();

            // assert if database are ok
            Assert(n);
        }

        static void RunTask(LiteEngine db)
        {

            //for(var i = 0; i < 10; i++)
            {
                var doc = new BsonDocument() { ["name"] = "testing - " + Guid.NewGuid() };

                db.Insert("collection", doc, BsonType.Int32);
            }

            db.Find("collection", Query.LTE("_id", 100)).ToArray();
            
            //doc["name"] = "changed name - " + Guid.NewGuid();
            
            //db.Update("collection", doc);

        }

        static void Assert(int n)
        {
            // checks if are ok
            using (var db = new LiteEngine(filename))
            {
                if (db.FindAll("collection").ToArray().Length != n)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Assert Fail - database file error");
                }
            }
        }

        #region Process/Thread Testing

        static void TestProcess(int n)
        {
            var tasks = new List<Task>();

            for (var i = 0; i < n; i++)
            {
                var t = Task.Factory.StartNew(() =>
                {
                    using (var db = InitDB())
                    {
                        RunTask(db);
                    }
                });

                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
        }

        static void TestThread(int n)
        {
            var tasks = new List<Task>();

            using (var db = InitDB())
            {
                for (var i = 0; i < n; i++)
                {
                    var t = Task.Factory.StartNew(() =>
                    {
                        RunTask(db);
                    });

                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());
            }

        }

        #endregion  
    }
}