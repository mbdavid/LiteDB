using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LiteDB.Engine;
using LiteDB.Tests;
using Xunit;

namespace LiteDB.Internals
{
    public class LockFile_Tests
    {
        [Fact]
        public void LockFile_MultiRead()
        {
            using(var l = new LockTest(TimeSpan.FromSeconds(60)))
            {
                l.Lock(LockMode.Read, 1000);
                l.Lock(LockMode.Read, 700);
                l.Lock(LockMode.Write, 1000);
                l.Lock(LockMode.Read, 800);
                l.Lock(LockMode.Read, 1500);
                l.Lock(LockMode.Write, 1000);

                l.Wait(); // all queue executed - delete lock-file
                
                l.Lock(LockMode.Read, 1000);
                l.Lock(LockMode.Read, 100);
                l.Lock(LockMode.Read, 2000);
                l.Lock(LockMode.Write, 2000);
                l.Lock(LockMode.Write, 500);
            }
        }

        [Fact]
        public void LockFile_RebuildTimeout()
        {
            using (var l = new LockTest(TimeSpan.FromSeconds(5)))
            {
                l.Lock(LockMode.Read, 1000);
                l.Lock(LockMode.Read, 500, true);
                l.Lock(LockMode.Write, 1000);

                Task.Delay(4000).Wait();

                l.Lock(LockMode.Write, 2500);
            }
        }

        class LockTest : IDisposable
        {
            private readonly TimeSpan _timeout;
            private readonly List<Task> _tasks = new List<Task>();
            private readonly TempFile _lock = new TempFile();
            private readonly TempFile _db = new TempFile();
            private int _id = 0;

            public LockTest(TimeSpan timeout)
            {
                _timeout = timeout;

                // create emtpy engine
                new LiteEngine(_db.Filename).Dispose();
            }

            public void Lock(LockMode mode, int delay, bool forceExit = false)
            {
                var id = ++_id;

                var task = Task.Run(() =>
                {
                    LiteEngine engine = null;

                    var locker = new ReadWriteLockFile(_lock.Filename, _timeout);

                    Debug.Print($"--> Task {id} entering {mode}");

                    locker.AcquireLock(mode, () =>
                    {
                        // start engine
                        engine = new LiteEngine(new EngineSettings
                        {
                            Filename = _db.Filename,
                            ReadOnly = mode == LockMode.Read
                        });
                    });

                    Debug.Print($">>> Task {id} entered {mode} (wait {delay} ms)");

                    Debug.Print($"--- Task {id} = {locker.Debug}");

                    Task.Delay(delay).Wait();

                    // dispose engine before release lock
                    engine.Dispose();

                    // for an exit when running (for testing)
                    // I dispose engine before to simulate closing another process
                    if (forceExit)
                    {
                        // dispose lock file too
                        locker.Dispose();
                        Debug.Print($"==> Task {id} aborting...");
                        return;
                    }

                    locker.ReleaseLock();

                    Debug.Print($"==> Task {id} release {mode}");

                    Debug.Print($"--- Task {id} = {locker.Debug}");

                    locker.Dispose();
                });

                Task.Delay(100).Wait();

                _tasks.Add(task);
            }

            public void Wait()
            {
                Task.WaitAll(_tasks.ToArray());
                Debug.Print("(waiting)");
            }

            public void Dispose()
            {
                Task.WaitAll(_tasks.ToArray());

                _lock.Dispose();
                _db.Dispose();
            }
        }
    }


}