using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public abstract class StressTest : IDisposable
    {
        // "fixed" random number order
        protected readonly Random rnd = new Random(0);

        public LiteEngine Engine { get; }

        private readonly LiteDatabase _db;

        private readonly Logger _logger;

        public bool Synced { get; set; } = false;

        public StressTest(EngineSettings settings)
        {
            settings.Seed = 0;
            settings.Timeout = TimeSpan.FromHours(1);

            if (settings.Filename != null)
            {
                File.Delete(settings.Filename);
                File.Delete(GetSufixFile(settings.Filename, "-log", false));
                File.Delete(GetSufixFile(settings.Filename, "-tmp", false));

                //_logger = new Logger(GetSufixFile(settings.Filename, "-eventLog", true));
            }
            //else
            {
                _logger = null;
            }

            this.Engine = new LiteEngine(settings);

            _db = new LiteDatabase(this.Engine);

            new DatabaseDebugger(_db).Start(8001);
        }

        public abstract void OnInit(Database db);

        public abstract void OnCleanUp(Database db);

        /// <summary>
        /// Run all methods
        /// </summary>
        public virtual void Run(TimeSpan timer)
        {
            var running = true;
            var watch = new Stopwatch();
            var concurrent = new ConcurrentCounter();
            var exec = 0;
            var paused = false;
            var waiter = new ManualResetEventSlim();

            Console.WriteLine("Start running: " + this.GetType().Name);

            // initialize database
            this.OnInit(new Database("OnInit", _db, _logger, watch, concurrent, 0));

            var tasks = new List<Task>();
            var methods = this.GetType()
                .GetMethods()
                .Where(x => x.GetCustomAttribute<TaskAttribute>() != null)
                .Select(x => new Tuple<MethodInfo, TaskAttribute>(x, x.GetCustomAttribute<TaskAttribute>()))
                .ToArray();

            watch.Start();

            foreach (var method in methods)
            {
                for (var i = 0; i < method.Item2.Threads; i++)
                {
                    var index = i;

                    // create one task per method
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        var count = 0;

                        // running loop
                        while (running && watch.Elapsed < timer)
                        {
                            var wait = count == 0 ? method.Item2.Start : method.Item2.Repeat;
                            var delay = wait + rnd.Next(0, method.Item2.Random);
                            var name = method.Item2.Threads == 1 ? method.Item1.Name : method.Item1.Name + "_" + index;

                            var sql = new Database(name, _db, _logger, watch, concurrent, index);

                            Task.Delay(delay).GetAwaiter().GetResult();

                            if (paused)
                            {
                                Console.WriteLine("Pausing thread #" + Task.CurrentId);
                                waiter.Wait();
                                Console.WriteLine("Running thread #" + Task.CurrentId);
                            }

                            try
                            {
                                if (this.Synced) Monitor.Enter(_db);

                                method.Item1.Invoke(this, new object[] { sql });

                                exec++;
                            }
                            catch (TargetInvocationException ex)
                            {
                                running = false;

                                Console.WriteLine($"ERROR {method.Item1.Name} : {ex.InnerException.Message}");
                            }
                            catch (Exception ex)
                            {
                                running = false;

                                Console.WriteLine($"ERROR {method.Item1.Name} : {ex.Message}");
                            }
                            finally
                            {
                                if (this.Synced) Monitor.Exit(_db);
                            }

                            count++;
                        }
                    }));
                }
            }

            // progress task
            tasks.Add(Task.Factory.StartNew(() =>
            {
                while (running && watch.Elapsed < timer)
                {
                    Console.WriteLine(string.Format("{0:00}:{1:00}:{2:00}: {3}",
                        watch.Elapsed.Hours,
                        watch.Elapsed.Minutes,
                        watch.Elapsed.Seconds,
                        exec));

                    Task.Delay(10000).GetAwaiter().GetResult();

                    if (paused) waiter.Wait();
                }
            }));

            // pause tasks
            tasks.Add(Task.Factory.StartNew(() =>
            {
                while(running && watch.Elapsed < timer)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.P)
                    {
                        if (paused == false)
                        {
                            Console.WriteLine(string.Format("{0:00}:{1:00}:{2:00}: {3}",
                                watch.Elapsed.Hours,
                                watch.Elapsed.Minutes,
                                watch.Elapsed.Seconds,
                                exec));

                            Console.WriteLine("[Paused]");
                            waiter.Reset();
                            paused = true;
                            watch.Stop();
                        }
                        else
                        {
                            Console.WriteLine("[Running]");
                            paused = false;
                            waiter.Set();
                            watch.Start();
                        }
                    }
                    else
                    {
                        running = false;
                    }
                }
            }));

            // wait finish all tasks
            Task.WaitAll(tasks.ToArray());

            // finalize database
            this.OnCleanUp(new Database("OnCleanUp", _db, _logger, watch, concurrent, 0));
        }

        /// <summary>
        /// Create a temp filename based on original filename - checks if file exists (if exists, append counter number)
        /// </summary>
        public static string GetSufixFile(string filename, string suffix = "-temp", bool checkIfExists = true)
        {
            var count = 0;
            var temp = Path.Combine(Path.GetDirectoryName(filename),
                Path.GetFileNameWithoutExtension(filename) + suffix +
                Path.GetExtension(filename));

            while (checkIfExists && File.Exists(temp))
            {
                temp = Path.Combine(Path.GetDirectoryName(filename),
                    Path.GetFileNameWithoutExtension(filename) + suffix +
                    "-" + (++count) +
                    Path.GetExtension(filename));
            }

            return temp;
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
