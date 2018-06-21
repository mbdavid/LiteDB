using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Lock service are collection-based locks. Lock will support any threads reading at same time. Writing operations will be locked
    /// based on collection. Eventualy, write operation can change header page that has an exclusive locker for.
    /// </summary>
    public class LockService : IDisposable
    {
        private TimeSpan _timeout;
        private Logger _log;

        private ReaderWriterLockSlim _transaction = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ConcurrentDictionary<string, ReaderWriterLockSlim> _collections = new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);
        private ReaderWriterLockSlim _reserved = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        internal LockService(TimeSpan timeout, Logger log)
        {
            _timeout = timeout;
            _log = log;
        }

        /// <summary>
        /// Return if current thread have open transaction
        /// </summary>
        public bool IsInTransaction => _transaction.IsReadLockHeld;

        /// <summary>
        /// Use ReaderWriterLockSlim to manage only one transaction per thread
        /// </summary>
        public void EnterTransaction()
        {
            try
            {
                if (_transaction.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("transaction", _timeout);
            }
            catch (LockRecursionException)
            {
                throw LiteException.InvalidTransactionState();
            }
        }

        /// <summary>
        /// Exit transaction locker
        /// </summary>
        public void ExitTransaction()
        {
            _transaction.ExitReadLock();
        }

        /// <summary>
        /// Enter collection in read lock mode
        /// </summary>
        public void EnterRead(string collectionName)
        {
            DEBUG(_transaction.IsReadLockHeld == false, "Use EnterTransaction() before EnterRead(name)");

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // try enter in read lock in collection
            if (collection.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("read", collectionName, _timeout);
        }

        /// <summary>
        /// Exit read lock
        /// </summary>
        public void ExitRead(string collectionName)
        {
            if (_collections.TryGetValue(collectionName, out var collection) == false) throw LiteException.InvalidTransactionState();

            collection.ExitReadLock();
        }

        /// <summary>
        /// Enter collection in reserved lock mode
        /// </summary>
        public void EnterReserved(string collectionName)
        {
            DEBUG(_transaction.IsReadLockHeld == false, "Use EnterTransaction() before EnterReserved(name)");

            // reserved locker in read lock (if not already reserved in this thread be another snapshot)
            if (_reserved.IsReadLockHeld == false && _reserved.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", collectionName, _timeout);

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // try enter in reserved lock in collection
            if (collection.TryEnterUpgradeableReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", collectionName, _timeout);
        }

        /// <summary>
        /// Exit reserved lock
        /// </summary>
        public void ExitReserved(string collectionName)
        {
            if (_collections.TryGetValue(collectionName, out var collection) == false) throw LiteException.InvalidTransactionState();

            collection.ExitUpgradeableReadLock();

            // in global reserved case, you can have same thread tring read-lock twice on different snapshot - exit once
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
            // wait finish all transactions before enter in reserved mode
            if (_transaction.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);

            try
            {
                // reserved locker in write lock
                if (_reserved.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);
            }
            finally
            {
                // exit exclusive and allow new readers
                _transaction.ExitWriteLock();
            }
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
        }

        public void Dispose()
        {
            _transaction.Dispose();
            _reserved.Dispose();
            _collections.ForEach((i, x) => x.Value.Dispose());
        }
    }
}