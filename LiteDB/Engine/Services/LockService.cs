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
        private ConcurrentDictionary<string, ReaderWriterLockSlim> _collections = new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);

        internal LockService(TimeSpan timeout, Logger log)
        {
            _timeout = timeout;
            _log = log;
        }

        /// <summary>
        /// Enter collection in read lock mode
        /// </summary>
        public void EnterRead(string collectionName)
        {
            // main locker in read lock
            if (_main.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("read", _timeout);

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // if current thread already has this lock, just exit
            if (collection.IsReadLockHeld) return;

            // try enter in read lock in collection
            if (collection.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("read", collectionName, _timeout);
        }

        /// <summary>
        /// Exit read lock
        /// </summary>
        public void ExitRead(string collectionName)
        {
            var collection = _collections[collectionName];

            if (collection.IsReadLockHeld)
            {
                collection.ExitReadLock();
            }

            if (_main.IsReadLockHeld)
            {
                _main.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter collection in reserved lock mode
        /// </summary>
        public void EnterReserved(string collectionName)
        {
            // main locker in read lock
            if (_main.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // if current thread already has this lock, just exit
            if (collection.IsUpgradeableReadLockHeld) return;

            // try enter in reserved lock in collection
            if (collection.TryEnterUpgradeableReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", collectionName, _timeout);
        }

        /// <summary>
        /// Exit reserved lock
        /// </summary>
        public void ExitReserved(string collectionName)
        {
            var collection = _collections[collectionName];

            if (collection.IsUpgradeableReadLockHeld)
            {
                collection.ExitUpgradeableReadLock();
            }

            if (_main.IsReadLockHeld)
            {
                _main.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter collection in write lock mode
        /// </summary>
        public void EnterWrite(string collectionName)
        {
            // main locker in read lock
            if (_main.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // if current thread already has this lock, just exit
            if (collection.IsWriteLockHeld);

            // try enter in write lock in collection
            if (collection.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("write", collectionName, _timeout);
        }

        /// <summary>
        /// Exit write lock
        /// </summary>
        public void ExitWrite(string collectionName)
        {
            var collection = _collections[collectionName];

            if (collection.IsWriteLockHeld)
            {
                collection.ExitWriteLock();
            }

            if (_main.IsReadLockHeld)
            {
                _main.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter in database exclusive mode - no others can read or write
        /// </summary>
        public void EnterExclusive()
        {
            // main locker in write lock
            if (_main.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("exclusive", _timeout);
        }

        /// <summary>
        /// Exit exclusive lock
        /// </summary>
        public void ExitExclusive()
        {
            if (_main.IsWriteLockHeld)
            {
                _main.ExitWriteLock();
            }
        }
    }
}