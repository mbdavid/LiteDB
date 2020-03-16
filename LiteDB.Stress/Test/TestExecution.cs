using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace LiteDB.Stress
{
    public class TestExecution
    {
        public TimeSpan Duration { get; }
        public Stopwatch Timer { get; } = new Stopwatch();

        private readonly TestFile _file;
        private LiteDatabase _db;
        private bool _running = true;
        private readonly ConcurrentDictionary<int, ThreadInfo> _threads = new ConcurrentDictionary<int, ThreadInfo>();

        public TestExecution(string filename, TimeSpan duration)
        {
            this.Duration = duration;

            _file = new TestFile(filename);
        }

        public void Execute()
        {
            if (_file.Delete)
            {
                this.DeleteFiles();
            }

            _db = new LiteDatabase(_file.Filename);

            // create all threads
            this.CreateThreads();

            // start report thread
            var t = new Thread(() => this.ReportThread());
            t.Name = "REPORT";
            t.Start();
        }

        private void DeleteFiles()
        {
            var searchPattern = Path.GetFileNameWithoutExtension(_file.Filename);
            var filesToDelete = Directory.GetFiles(".", $"{searchPattern}*" + Path.GetExtension(_file.Filename));

            foreach (var deleteFile in filesToDelete)
            {
                File.Delete(deleteFile);
            }

            File.Delete(_file.Output);
        }

        private void CreateThreads()
        {
            foreach (var task in _file.Tasks)
            {
                for (var i = 0; i < task.TaskCount; i++)
                {
                    var thread = new Thread(() =>
                    {
                        while (true)
                        {
                            var info = _threads[Thread.CurrentThread.ManagedThreadId];
                            info.CancellationTokenSource.Token.WaitHandle.WaitOne(task.Sleep);
                            if (info.CancellationTokenSource.Token.IsCancellationRequested) break;
                            info.Elapsed.Restart();
                            info.Running = true;
                            try
                            {
                                info.Result = task.Execute(_db);
                            }
                            catch (Exception ex)
                            {
                                info.Exception = ex;
                                _running = false;
                                break;
                            }
                            info.Running = false;
                            info.Elapsed.Stop();
                            info.Counter++;
                            info.LastRun = DateTime.Now;
                        }
                    });

                    _threads[thread.ManagedThreadId] = new ThreadInfo
                    {
                        Task = task,
                        Thread = thread
                    };

                    thread.Name = task.Name;
                    thread.Start();
                }
            }
        }

        private void ReportThread()
        {
            this.Timer.Start();

            var output = new StringBuilder();

            while(this.Timer.Elapsed < this.Duration && _running)
            {
                Thread.Sleep(Math.Min(1000, (int)this.Duration.Subtract(this.Timer.Elapsed).TotalMilliseconds));

                output.Clear();

                output.AppendLine($"LiteDB Multithreaded: {_threads.Count}, running for {this.Timer.Elapsed}");
                output.AppendLine();

                foreach (var thread in _threads)
                {
                    var howLong = DateTime.Now - thread.Value.LastRun;

                    var id = thread.Key.ToString("00");
                    var name = (thread.Value.Task.Name + (thread.Value.Running ? "*" : "")).PadRight(15, ' ');
                    var counter = thread.Value.Counter.ToString().PadRight(5, ' ');
                    var timer = howLong.TotalSeconds > 60 ?
                        ((int)howLong.TotalMinutes).ToString().PadLeft(2, ' ') + " minutes" :
                        ((int)howLong.TotalSeconds).ToString().PadLeft(2, ' ') + " seconds";
                    var result = thread.Value.Result != null ? $"[{thread.Value.Result.ToString()}]" : "";
                    var running = thread.Value.Elapsed.Elapsed.TotalSeconds > 1 ?
                        $"<LAST RUN {(int)thread.Value.Elapsed.Elapsed.TotalSeconds}s> " :
                        "";
                    var ex = thread.Value.Exception != null ?
                        " ERROR: " + thread.Value.Exception.Message :
                        "";

                    output.AppendLine($"{id}. {name} :: {counter} >> {timer} {running}{result}{ex}");
                }

                Console.Clear();
                Console.WriteLine(output.ToString());
            }

            this.StopRunning();

            _db.Dispose();

            File.AppendAllText(_file.Output, output.ToString());
        }

        private void StopRunning()
        {
            foreach (var t in _threads.Values)
            {
                t.CancellationTokenSource.Cancel();
                t.Thread.Join();
            }
        }
    }
}
