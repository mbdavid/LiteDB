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
        protected Random Rnd { get; } = new Random(0);
        protected TimeSpan Timer { get; private set; }

        private readonly LiteDatabase _db;
        private readonly Logger _logger;
        private readonly bool _synced = false;
        private readonly DatabaseDebugger _debugger;

        public StressTest(EngineSettings settings, bool synced)
        {
            settings.Timeout = TimeSpan.FromHours(1);

            if (settings.Filename != null)
            {
                File.Delete(settings.Filename);
                File.Delete(GetSufixFile(settings.Filename, "-log", false));
            }

            var engine = new LiteEngine(settings);

            _db = new LiteDatabase(engine);

            _debugger = new DatabaseDebugger(_db);

            _debugger.Start(8001);
        }

        public abstract void OnInit(DbContext db);

        public abstract void OnCleanUp(DbContext db);

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

            this.Timer = timer;

            // initialize database
            this.OnInit(new DbContext("OnInit", 0, _db, _logger, watch, concurrent));

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

                        var context = new DbContext(method.Item1.Name, index, _db, _logger, watch, concurrent);

                        // running loop
                        while (running && watch.Elapsed < timer)
                        {
                            var wait = count == 0 ? method.Item2.Start : method.Item2.Repeat;
                            var delay = wait + this.Rnd.Next(0, method.Item2.Random);

                            Task.Delay(delay).GetAwaiter().GetResult();

                            if (running == false || watch.Elapsed > timer) break;

                            if (paused) waiter.Wait();

                            try
                            {
                                if (_synced) Monitor.Enter(_db);

                                method.Item1.Invoke(this, new object[] { context });

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
                                if (_synced) Monitor.Exit(_db);
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
                    Task.Delay(250).GetAwaiter().GetResult();

                    if (Console.KeyAvailable == false) continue;

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
                }
            }));

            // wait finish all tasks
            Task.WaitAll(tasks.ToArray());

            // finalize database
            this.OnCleanUp(new DbContext("OnInit", 0, _db, _logger, watch, concurrent));
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
            _debugger.Dispose();

            _db.Dispose();
        }
    }
}
