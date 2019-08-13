using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// https://stackoverflow.com/a/53787485/3286260
    /// </summary>
    internal class CrossProcessReaderWriterLock
    {
        private const int MAX_READERS = 100;

        private readonly string _name;
        private readonly Mutex _mutex;
        private readonly Semaphore _semaphore;

        public CrossProcessReaderWriterLock(string name)
        {
            _name = name;
            _mutex = new Mutex(false, name + ".Mutex");
            _semaphore = new Semaphore(MAX_READERS, MAX_READERS, name + ".Semaphore");
        }

        public void AcquireReaderLock()
        {
            _mutex.WaitOne();
            _semaphore.WaitOne();
            _mutex.ReleaseMutex();
        }

        public void ReleaseReaderLock()
        {
            _semaphore.Release();
        }

        public void AcquireWriterLock()
        {
            _mutex.WaitOne();

            for (int i = 0; i < MAX_READERS; i++)
            {
                _semaphore.WaitOne(); // drain out all readers-in-progress
            }

            _mutex.ReleaseMutex();
        }

        public void ReleaseWriterLock()
        {
            for (int i = 0; i < MAX_READERS; i++)
            {
                _semaphore.Release();
            }
        }
    }
}