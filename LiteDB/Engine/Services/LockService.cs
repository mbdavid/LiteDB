using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// Lock service are collection-based locks. Lock will support any threads reading at same time. Writing operations will be locked
    /// based on collection. Eventualy, write operation can change header page that has an exclusive locker for.
    /// </summary>
    public class LockService
    {
        private TimeSpan _timeout;
        private Logger _log;

        private ConcurrentDictionary<string, ReaderWriterLockSlim> _collections = new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);
        private ReaderWriterLockSlim _main = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ReaderWriterLockSlim _header = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        internal LockService(TimeSpan timeout, Logger log)
        {
            _timeout = timeout;
            _log = log;
        }

        /// <summary>
        /// Lock current thread in read mode
        /// </summary>
        public LockReadWrite Read()
        {
            // main locker in read lock
            _main.TryEnterReadLock(_timeout);

            return new LockReadWrite(_main, _log);
        }

        /// <summary>
        /// Lock current thread in read mode + get collection locker to to write-lock
        /// </summary>
        public void Write(LockReadWrite locker, string collectionName)
        {
            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // if current thread already has this lock, just exit
            if (collection.IsWriteLockHeld) return;

            // lock collectionName in write mode
            collection.TryEnterWriteLock(_timeout);

            locker.Collections.Add(collection);
        }

        /// <summary>
        /// Lock header page in write-mode. Need be inside a write lock collection. 
        /// Will release header locker only when dispose collection locker
        /// </summary>
        public void Header(LockReadWrite locker)
        {
            // are this thread already in header lock-write? exit
            if (_header.IsWriteLockHeld) return;

            // lock-write header locker
            _header.TryEnterWriteLock(_timeout);

            locker.Header = _header;
        }

        /// <summary>
        /// Do a exclusive read/write lock for all other threads. Only this thread can use database (for some WAL/Shrink operations)
        /// </summary>
        public LockExclusive Exclusive()
        {
            // write lock in main locker
            _main.TryEnterWriteLock(_timeout);

            return new LockExclusive(_main);
        }
    }
}