using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// ThreadSafe
    /// </summary>
    internal class FileMemoryCache : IDisposable
    {
        private readonly MemoryStore _memory;
        private readonly ConcurrentDictionary<long, PageBuffer> _cache = new ConcurrentDictionary<long, PageBuffer>();
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        private readonly ManualResetEventSlim _waiter = new ManualResetEventSlim(false);
        private Thread _thread = null;
        private bool _running = true;
        private int _checkLimitSize = MEMORY_FILE_CACHE_SIZE;

        public FileMemoryCache(MemoryStore memory)
        {
            _memory = memory;
        }

        public PageBuffer GetOrAddPage(long position, Func<long, PageBuffer> addFactory)
        {
            _locker.EnterReadLock();

            try
            {
                return _cache.GetOrAdd(position, (p) =>
                {
                    this.RunCleanup();
                    return addFactory(p);
                });
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public bool TryGetPage(long position, out PageBuffer page)
        {
            _locker.EnterReadLock();

            try
            {
                return _cache.TryGetValue(position, out page);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public void AddPage(PageBuffer page)
        {
            _locker.EnterReadLock();

            try
            {
                _cache.AddOrUpdate(page.Position, page, (pos, old) =>
                {
                    this.RunCleanup();
                    return page;
                });
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Run cleanup if cache usage was bigger than last defined limit (run in another thread)
        /// Otherwise, just exit
        /// </summary>
        private void RunCleanup()
        {
            // cache are not full
            if (_cache.Count < _checkLimitSize) return;

            // if thread are not created yet, do now and run first time
            if (_thread == null)
            {
                _thread = new Thread(this.CreateThread);
                _thread.Name = "LiteDB_CacheCleaner";
                _thread.Start();
            }
            else
            {
                // thread already running
                if (_waiter.IsSet) return;

                _waiter.Set();
            }
        }

        private void CreateThread()
        {
            while (_running)
            {
                // avoid cache access during cleanup
                _locker.EnterWriteLock();

                try
                {
                    // get all pages that are not been shared
                    var toDelete = _cache.Values
                        .Where(x => x.ShareCounter == 0)
                        .Select(x => x.Position)
                        .ToArray();

                    foreach (var position in toDelete)
                    {
                        if(_cache.TryRemove(position, out var page))
                        {
                            _memory.Return(ref page.Buffer);
                        }
                    }

                    // define new check limit based on how many items contains in cache + fixed size
                    _checkLimitSize = _cache.Count + MEMORY_FILE_CACHE_SIZE;
                }
                finally
                {
                    _locker.ExitWriteLock();
                }

                // suspend thread and wait another signal
                _waiter.Reset();

                if (!_running) return;

                _waiter.Wait();
            }
        }

        public void Dispose()
        {
            // stop running async task in next while check
            _running = false;

            if (_waiter.IsSet == false)
            {
                _waiter.Set();
            }

            // wait thread
            _thread?.Join();
            _locker.Dispose();
        }
    }
}