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
    /// [ThreadSafe]
    /// </summary>
    internal class LockService : IDisposable
    {
        private readonly EnginePragmas _pragmas;

        private readonly ReaderWriterLockSlim _transaction = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly ConcurrentDictionary<string, object> _collections = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        internal LockService(EnginePragmas pragmas)
        {
            _pragmas = pragmas;
        }

        /// <summary>
        /// Return if current thread have open transaction
        /// </summary>
        public bool IsInTransaction => _transaction.IsReadLockHeld || _transaction.IsWriteLockHeld;

        /// <summary>
        /// Return how many transactions are opened
        /// </summary>
        public int TransactionsCount => _transaction.CurrentReadCount;

        /// <summary>
        /// Enter transaction read lock - should be called just before enter a new transaction
        /// </summary>
        public void EnterTransaction()
        {
            // if current thread already in exclusive mode, just exit
            if (_transaction.IsWriteLockHeld) return;

            if (_transaction.TryEnterReadLock(_pragmas.Timeout) == false) throw LiteException.LockTimeout("transaction", _pragmas.Timeout);
        }

        /// <summary>
        /// Exit transaction read lock
        /// </summary>
        public void ExitTransaction()
        {
            // if current thread are in reserved mode, do not exit transaction (will be exit from ExitExclusive)
            if (_transaction.IsWriteLockHeld) return;

            _transaction.ExitReadLock();
        }

        /// <summary>
        /// Enter collection write lock mode (only 1 collection per time can have this lock)
        /// </summary>
        public void EnterLock(string collectionName)
        {
            ENSURE(_transaction.IsReadLockHeld || _transaction.IsWriteLockHeld, "Use EnterTransaction() before EnterLock(name)");

            // get collection object lock from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new object());

            if (Monitor.TryEnter(collection, _pragmas.Timeout) == false) throw LiteException.LockTimeout("write", collectionName, _pragmas.Timeout);
        }

        /// <summary>
        /// Exit collection in reserved lock
        /// </summary>
        public void ExitLock(string collectionName)
        {
            if (_collections.TryGetValue(collectionName, out var collection) == false) throw LiteException.CollectionLockerNotFound(collectionName);

            Monitor.Exit(collection);
        }

        /// <summary>
        /// Enter all database in exclusive lock. Wait for all transactions finish. In exclusive mode no one can enter in new transaction (for read/write)
        /// If current thread already in exclusive mode, returns false
        /// </summary>
        public bool EnterExclusive()
        {
            // if current thread already in exclusive mode
            if (_transaction.IsWriteLockHeld) return false;

            // wait finish all transactions before enter in reserved mode
            if (_transaction.TryEnterWriteLock(_pragmas.Timeout) == false) throw LiteException.LockTimeout("exclusive", _pragmas.Timeout);

            return true;
        }

        /// <summary>
        /// Exit exclusive lock
        /// </summary>
        public void ExitExclusive()
        {
            _transaction.ExitWriteLock();
        }

        public void Dispose()
        {
            try
            {
                _transaction.Dispose();
            }
            catch (SynchronizationLockException)
            {
            }
        }
    }
}