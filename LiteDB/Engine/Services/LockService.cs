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

        private ReaderWriterLockSlim _main = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ReaderWriterLockSlim _reserved = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ConcurrentDictionary<string, ReaderWriterLockSlim> _collections = new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);

        internal LockService(TimeSpan timeout, Logger log)
        {
            _timeout = timeout;
            _log = log;
        }

        /// <summary>
        /// Enter in read lock
        /// </summary>
        public LockReadWrite Read()
        {
            _log.LockRead(_main);

            // main locker in read lock
            if (_main.TryEnterReadLock(_timeout) == false)
            {
                throw LiteException.LockTimeout("read", _timeout);
            }

            return new LockReadWrite(_main, _log);
        }

        /// <summary>
        /// Enter in write lock - only single thread can write in this collection (but all others can read)
        /// </summary>
        public void Write(LockReadWrite locker, string collectionName)
        {
            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // if current thread already has this lock, just exit
            if (collection.IsWriteLockHeld) return;

            _log.LockWrite(collection, collectionName);

            // lock collectionName in write mode
            if (collection.TryEnterWriteLock(_timeout) == false)
            {
                throw LiteException.LockTimeout("write '" + collectionName + "'", _timeout);
            }

            locker.Collections.Add(new Tuple<string, ReaderWriterLockSlim>(collectionName, collection));
        }

        /// <summary>
        /// Enter in reserved lock - only single thread can be in reserved mode (but all others can read)
        /// </summary>
        public void Reserved(LockReadWrite locker)
        {
            // if current thread already has write-lock in collection page in lock
            if (_reserved.IsWriteLockHeld) return;

            _log.LockReserved(_reserved);

            // lock-write reserved locker
            if (_reserved.TryEnterWriteLock(_timeout) == false)
            {
                throw LiteException.LockTimeout("reserved", _timeout);
            }

            locker.Reserved = _reserved;
        }

        /// <summary>
        /// Do a exclusive read/write lock for all other threads. Only this thread can use database (for some WAL/Shrink operations)
        /// </summary>
        public LockExclusive Exclusive()
        {
            _log.LockExclusive(_main);

            // write lock in main locker - use higher timespan because run in async task
            if (_main.TryEnterWriteLock(TimeSpan.FromHours(1)) == false)
            {
                throw LiteException.LockTimeout("exclusive", TimeSpan.FromHours(1));
            }

            return new LockExclusive(_main, _log);
        }
    }
}