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
        public void MultiRead_LockFile()
        {
            using(var l = new LockTest())
            {
                l.Lock(LockMode.Read, 1000);
                l.Lock(LockMode.Read, 700);
                l.Lock(LockMode.Write, 1000);
                l.Lock(LockMode.Read, 800);
                l.Lock(LockMode.Read, 1500);
                l.Lock(LockMode.Write, 1000);

                l.Wait();

                l.Lock(LockMode.Read, 1000);
                //l.Lock(LockMode.Read, 100);
                //l.Lock(LockMode.Read, 2000);
                //l.Lock(LockMode.Write, 2000);
                //l.Lock(LockMode.Write, 500);


            }


        }

        class LockTest : IDisposable
        {
            private TimeSpan _timeout = TimeSpan.FromSeconds(60);
            private List<Task> _tasks = new List<Task>();
            private TempFile _file = new TempFile();
            private int _id = 0;

            public void Lock(LockMode mode, int delay)
            {
                var id = ++_id;

                var task = Task.Run(() =>
                {
                    using (var l = new ReadWriteLockFile(_file.Filename, _timeout))
                    {
                        Debug.Print($"--> Task {id} entering {mode}");

                        l.AcquireLock(mode);

                        Debug.Print($">>> Task {id} entered {mode} (wait {delay} ms)");

                        Debug.Print($"--- Task {id} = {l.Debug}");

                        Task.Delay(delay).Wait();

                        l.ReleaseLock();

                        Debug.Print($"==> Task {id} release {mode}");

                        Debug.Print($"--- Task {id} = {l.Debug}");
                    }
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

                _file.Dispose();
            }
        }
    }


}