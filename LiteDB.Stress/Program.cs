using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Stress
{
    public class Program
    {
        private const string DB_FILENAME = ".\\stress-test.db";

        private const int INITAL_INSERT_THREADS = 10;
        private const int INSERT_SLEEP = 10;

        private static ConcurrentDictionary<int, ThreadInfo> _threads = new ConcurrentDictionary<int, ThreadInfo>();
        private static LiteEngine _engine;
        private static Stopwatch _timer = Stopwatch.StartNew();

        static void Main()
        {
            ClearFiles();

            _engine = new LiteEngine(DB_FILENAME);

            CreateThread("REPORT", 1_000, ReportThread);
            CreateThread("CHECKPOINT", 5_000, () => _engine.Checkpoint());
            CreateThread("COUNT_1", 2_000, () => _engine.Query("col1", new Query { Select = "{ c: COUNT(*) }" }).ToArray()[0]["c"]);
            CreateThread("COUNT_2", 2_000, () => _engine.Query("col2", new Query { Select = "{ c: COUNT(*) }" }).ToArray()[0]["c"]);

            CreateThread("ENSURE_INDEX", 25_000, () => _engine.EnsureIndex("col1", "idx_ts", "$.timespan", false));
            CreateThread("DROP_INDEX", 60_000, () => _engine.DropIndex("col1", "idx_ts"));

            CreateThread("DELETE_ALL", 10 * 60 * 1_000, () => _engine.DeleteMany("col1", "1=1"));

            CreateThread("REBUILD", 11 * 60 * 1_000, () => _engine.Rebuild(new RebuildOptions()));

            CreateThread("FILE_SIZE", 1_000, () => _engine.Query("$database", new Query { Select = "{ data: FORMAT(dataFileSize, 'n0'), log: FORMAT(logFileSize, 'n0') }" }).ToArray()[0]);

            for (int i = 0; i < INITAL_INSERT_THREADS; ++i)
            {
                CreateThread("INSERT_1", INSERT_SLEEP, () => InsertThread("col1"));
            }

            CreateThread("INSERT_2", INSERT_SLEEP, () => InsertThread("col2"));
            CreateThread("INSERT_2", INSERT_SLEEP, () => InsertThread("col2"));
            CreateThread("INSERT_2", INSERT_SLEEP, () => InsertThread("col2"));

            while (true)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Spacebar)
                {
                    CreateThread("INSERT_1", INSERT_SLEEP, () => InsertThread("col1"));
                }

                if (key == ConsoleKey.Enter)
                {
                    foreach(var t in _threads.Values)
                    {
                        t.CancellationTokenSource.Cancel();
                        t.Thread.Join();
                    }
                    break;
                }
            }
        }

        static void ClearFiles()
        {
            var searchPattern = Path.GetFileNameWithoutExtension(DB_FILENAME);
            var filesToDelete = Directory.GetFiles(".", $"{searchPattern}*.db");

            foreach (var deleteFile in filesToDelete)
            {
                Console.WriteLine($"Deleting {deleteFile}");
                File.Delete(deleteFile);
            }
        }

        private static void CreateThread(string name, int sleep, Action fn)
        {
            CreateThread(name, sleep, () => { fn(); return null; });
        }

        private static void CreateThread(string name, int sleep, Func<BsonValue> fn)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    var info = _threads[Thread.CurrentThread.ManagedThreadId];
                    info.CancellationTokenSource.Token.WaitHandle.WaitOne(sleep);
                    if (info.CancellationTokenSource.Token.IsCancellationRequested) break;
                    info.Elapsed.Restart();
                    info.Running = true;
                    info.Result = fn();
                    info.Running = false;
                    info.Elapsed.Stop();
                    info.Counter++;
                    info.LastRun = DateTime.Now;
                }
            });

            _threads[thread.ManagedThreadId] = new ThreadInfo 
            { 
                Name = name, 
                Thread = thread,
            };

            thread.Name = name;
            thread.Start();
        }

        private static void InsertThread(string col)
        {
            _engine.Insert(col, new BsonDocument[]
            {
                new BsonDocument
                {
                    ["timespan"] = DateTime.Now
                }
            }, BsonAutoId.Int32);
        }

        private static void ReportThread()
        {
            Console.Clear();
            Console.WriteLine($"LiteDB Multithreaded: {_threads.Count}, running for {_timer.Elapsed}");
            Console.WriteLine("Press <ENTER> to stop processing, <SPACE> to add insert thread");
            Console.WriteLine();

            foreach (var thread in _threads)
            {
                var howLong = DateTime.Now - thread.Value.LastRun;

                var id = thread.Key.ToString("00");
                var name = (thread.Value.Name + (thread.Value.Running ? "*" : "")).PadRight(13, ' ');
                var counter = thread.Value.Counter.ToString().PadRight(5, ' ');
                var timer = howLong.TotalSeconds > 60 ?
                    ((int)howLong.TotalMinutes).ToString().PadLeft(2, ' ') + " minutes" :
                    ((int)howLong.TotalSeconds).ToString().PadLeft(2, ' ') + " seconds";
                var result = thread.Value.Result != null ? $"[{thread.Value.Result.ToString()}]" : "";
                var running = thread.Value.Elapsed.Elapsed.TotalSeconds > 1 ?
                    $"<LAST RUN {(int)thread.Value.Elapsed.Elapsed.TotalSeconds}s> " :
                    "";

                Console.WriteLine($"{id}. {name} :: {counter} >> {timer} {running}{result}" );
            }
        }
    }

    class ThreadInfo
    {
        public string Name { get; set; }
        public int Counter { get; set; } = 0;
        public bool Running { get; set;  } = false;
        public Stopwatch Elapsed { get; } = new Stopwatch();
        public DateTime LastRun { get; set; } = DateTime.Now;
        public BsonValue Result { get; set; }
        public Thread Thread { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
    }
}
