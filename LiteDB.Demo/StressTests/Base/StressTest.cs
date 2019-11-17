using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public abstract class StressTest : IDisposable
    {
        private readonly Random _rnd = new Random();

        private readonly LiteRepository _db;

        private readonly Logger _logger;

        public StressTest(string connectionString, Logger logger)
        {
            _db = new LiteRepository(connectionString);

            _logger = logger;
        }

        public abstract void OnInit(SqlDB db);

        /// <summary>
        /// Run all methods
        /// </summary>
        public void Run(TimeSpan timer)
        {
            var running = true;
            var finish = DateTime.Now.Add(timer);
            var watch = new Stopwatch();
            var concurrent = new ConcurrentCounter();
            var exec = 0;

            Console.WriteLine("Start running: " + this.GetType().Name);

            // setting log name
            _logger.Initialize(this.GetType().Name + "_Log");

            // initialize database
            this.OnInit(new SqlDB("OnInit", _db.Database, _logger, watch, concurrent, 0));

            var tasks = new List<Task>();
            var methods = this.GetType()
                .GetMethods()
                .Where(x => x.GetCustomAttribute<TaskAttribute>() != null)
                .Select(x => new Tuple<MethodInfo, TaskAttribute>(x, x.GetCustomAttribute<TaskAttribute>()))
                .ToArray();

            watch.Start();

            foreach(var method in methods)
            {
                for(var i = 0; i < method.Item2.Tasks; i++)
                {
                    var index = i;

                    // create one task per method
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        var count = 0;

                        // running loop
                        while (running && DateTime.Now < finish)
                        {
                            var wait = count == 0 ? method.Item2.Delay : method.Item2.Wait;
                            var delay = wait + _rnd.Next(0, method.Item2.Random);
                            var name = method.Item2.Tasks == 1 ? method.Item1.Name : method.Item1.Name + "_" + index;

                            var sql = new SqlDB(name, _db.Database, _logger, watch, concurrent, index);

                            Task.Delay(delay).GetAwaiter().GetResult();

                            try
                            {
                                method.Item1.Invoke(this, new object[] { sql });

                                exec++;
                            }
                            catch (Exception ex)
                            {
                                running = false;

                                Console.WriteLine($"ERROR {method.Item1.Name} : {ex.Message}");
                            }

                            count++;
                        }
                    }));
                }
            }

            // progress task
            tasks.Add(Task.Factory.StartNew(() =>
            {
                while (running && DateTime.Now < finish)
                {
                    Task.Delay(10000).GetAwaiter().GetResult();

                    Console.WriteLine(string.Format("{0:00}:{1:00}:{2:00}: {3}",
                        watch.Elapsed.Hours,
                        watch.Elapsed.Minutes,
                        watch.Elapsed.Seconds,
                        exec));
                }
            }));


            // wait finish all tasks
            Task.WaitAll(tasks.ToArray());

        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
