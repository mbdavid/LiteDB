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
        static string filename = Path.Combine(Path.GetTempPath(), "file_demo.db");
        static Logger log = new Logger(Logger.NONE, (s) => Console.WriteLine("#" + Thread.CurrentThread.ManagedThreadId.ToString("00") + " " + s));
        static int TASKS = 300;

        static void Main(string[] args)
        {
            // log.Level = Logger.DISK | Logger.LOCK;

            ExecuteTest("Process", TestProcess);
            ExecuteTest("Thread", TestThread);

            Console.WriteLine("End");
            Console.ReadKey();
        }

        static LiteEngine InitDB()
        {
            var disk = new FileDiskService(filename, true);
            return new LiteEngine(disk, null, null, 5000, log);
        }

        static void ExecuteTest(string name, Action test)
        {
            Console.WriteLine("{0} (N = {1})", name, TASKS);

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
            test();

            s.Stop();

            Console.WriteLine("Time Elapsed (ms): " + s.ElapsedMilliseconds);
            Console.WriteLine();

            // assert if database are ok
            Assert();
        }

        static void RunTask(LiteEngine db)
        {
            for(var i = 0; i < 10; i++)
            {
                var doc = new BsonDocument() { ["name"] = "testing - " + Guid.NewGuid() };

                db.Insert("collection", doc, BsonType.Int32);

                doc["name"] = "changed name - " + Guid.NewGuid();

                db.Update("collection", doc);
            }

            db.Find("collection", Query.LTE("_id", 100)).ToArray();
        }

        static void Assert()
        {
            // checks if are ok
            using (var db = new LiteEngine(filename))
            {
                db.FindAll("collection").ToArray();
            }
        }

        #region Process/Thread Testing

        static void TestProcess()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < TASKS; i++)
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

        static void TestThread()
        {
            var tasks = new List<Task>();

            using (var db = InitDB())
            {
                for (var i = 0; i < TASKS; i++)
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