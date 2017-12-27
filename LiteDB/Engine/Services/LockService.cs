using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Lock service are collection-based locks. Lock will support any threads reading at same time. Writing operations will be locked
    /// based on collection. Eventualy, write operation can change header page that has an exclusive locker for.
    /// </summary>
    public class LockService
    {
        private TimeSpan _timeout;

        private ConcurrentDictionary<string, LockCollection> _collections = new ConcurrentDictionary<string, LockCollection>(StringComparer.OrdinalIgnoreCase);
        private ReaderWriterLockSlim _main = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ReaderWriterLockSlim _header = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        internal LockService(TimeSpan timeout, Logger log)
        {
            _timeout = timeout;
        }

        /// <summary>
        /// Lock current thread in read mode
        /// </summary>
        public LockControl Read()
        {
            // main locker in read lock
            _main.TryEnterReadLock(_timeout);

            return new LockControl(() =>
            {
                _main.ExitReadLock();
            });
        }

        /// <summary>
        /// Lock current thread in read mode + get collection locker to to write-lock
        /// </summary>
        public LockControl Write(string collectionName)
        {
            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new LockCollection { Locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion), Header = false });

            // if current thread already in write lock, do nothig
            if (collection.Locker.IsWriteLockHeld) return new LockControl();

            // lock collectionName in write mode
            collection.Locker.TryEnterWriteLock(_timeout);

            // also, lock main locker in read mode
            _main.TryEnterReadLock(_timeout);

            // release both when dispose
            return new LockControl(() =>
            {
                collection.Locker.ExitWriteLock();

                // if collection
                if (collection.Header)
                {
                    _header.ExitWriteLock();
                }

                _main.ExitReadLock();
            });
        }

        /// <summary>
        /// Lock header page in write-mode. Need be inside a write lock collection. 
        /// Will release header locker only when dispose collection locker
        /// </summary>
        public void Header(string collectionName)
        {
            // are this thread already in header lock-write? exit
            if (_header.IsWriteLockHeld) return;

            // lock-write header locker
            _header.TryEnterReadLock(_timeout);

            // get current lock from 
            if (_collections.TryGetValue(collectionName, out var collection))
            {
                // mark current thread that will need release header lock when release collection locker
                collection.Header = true;
            }
            else
            {
                throw new LiteException("Header lock must be do inside a collection lock");
            }
        }

        /// <summary>
        /// Do a exclusive read/write lock for all other threads. Only this thread can use database (for some WAL/Shrink operations)
        /// </summary>
        public LockControl Exclusive()
        {
            // write lock in main locker
            _main.TryEnterWriteLock(_timeout);

            return new LockControl(() =>
            {
                _main.ExitWriteLock();
            });
        }

        private class LockCollection
        {
            public ReaderWriterLockSlim Locker { get; set; }
            public bool Header { get; set; }
        }
    }
}