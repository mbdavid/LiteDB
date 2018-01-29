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
        private ReaderWriterLockSlim _reserved = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ReaderWriterLockSlim _exclusive = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

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
            // if current thread already in exclusive or reserved mode, just exit
            if (_exclusive.IsWriteLockHeld || _reserved.IsWriteLockHeld) return;

            // exclusive locker in read lock
            if (_exclusive.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("read", collectionName, _timeout);

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

            if (_exclusive.IsReadLockHeld)
            {
                _exclusive.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter collection in reserved lock mode
        /// </summary>
        public void EnterReserved(string collectionName)
        {
            // if current thread already in exclusive or reserved mode, just exit
            if (_exclusive.IsWriteLockHeld || _reserved.IsWriteLockHeld) return;

            // reserved locker in read lock (if not already reserved in this thread)
            if (_reserved.IsReadLockHeld == false && _reserved.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", collectionName, _timeout);

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

            if (_reserved.IsReadLockHeld)
            {
                _reserved.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter all database in reserved lock - all others can read but not write
        /// </summary>
        public void EnterReserved()
        {
            // if current thread already in exclusive or reserved mode, just exit
            if (_exclusive.IsWriteLockHeld || _reserved.IsWriteLockHeld) return;

            // exclusive locker in read lock
            if (_exclusive.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);

            // reserved locker in write lock
            if (_reserved.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);
        }

        /// <summary>
        /// Exit reserved lock
        /// </summary>
        public void ExitReserved()
        {
            if (_reserved.IsWriteLockHeld)
            {
                _reserved.ExitWriteLock();
            }

            if (_exclusive.IsReadLockHeld)
            {
                _exclusive.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter in database exclusive mode - no others can read or write
        /// </summary>
        public void EnterExclusive()
        {
            // exclusive locker in write lock
            if (_exclusive.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("exclusive", _timeout);
        }

        /// <summary>
        /// Exit exclusive lock
        /// </summary>
        public void ExitExclusive()
        {
            if (_exclusive.IsWriteLockHeld)
            {
                _exclusive.ExitWriteLock();
            }
        }
    }
}