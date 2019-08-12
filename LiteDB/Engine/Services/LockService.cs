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
    public class LockService : IDisposable
    {
        private readonly TimeSpan _timeout;
        private readonly bool _readonly;

        private ReaderWriterLockSlim _transaction = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private ConcurrentDictionary<string, ReaderWriterLockSlim> _collections = new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);
        private ReaderWriterLockSlim _reserved = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        internal LockService(TimeSpan timeout, bool @readonly)
        {
            _timeout = timeout;
            _readonly = @readonly;
        }

        /// <summary>
        /// Return if current thread have open transaction
        /// </summary>
        public bool IsInTransaction => _transaction.IsReadLockHeld;

        /// <summary>
        /// Return how many transactions are opened
        /// </summary>
        public int TransactionsCount => _transaction.CurrentReadCount;

        /// <summary>
        /// Enter transaction read lock
        /// </summary>
        public void EnterTransaction()
        {
            // if current thread are in reserved mode, do not enter in transaction
            if (_transaction.IsWriteLockHeld) return;

            try
            {
                if (_transaction.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("transaction", _timeout);
            }
            catch (LockRecursionException)
            {
                throw LiteException.AlreadyExistsTransaction();
            }
        }

        /// <summary>
        /// Exit transaction read lock
        /// </summary>
        public void ExitTransaction()
        {
            // if current thread are in reserved mode, do not exit transaction (will be exit reserved lock)
            if (_transaction.IsWriteLockHeld) return;

            _transaction.ExitReadLock();
        }

        /// <summary>
        /// Enter collection in read lock
        /// </summary>
        public void EnterRead(string collectionName)
        {
            ENSURE(_transaction.IsReadLockHeld || _transaction.IsWriteLockHeld, "Use EnterTransaction() before EnterRead(name)");

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // try enter in read lock in collection
            if (collection.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("read", collectionName, _timeout);
        }

        /// <summary>
        /// Exit collection read lock
        /// </summary>
        public void ExitRead(string collectionName)
        {
            if (_collections.TryGetValue(collectionName, out var collection) == false) throw LiteException.CollectionLockerNotFound(collectionName);

            collection.ExitReadLock();
        }

        /// <summary>
        /// Enter collection reserved lock mode (only 1 collection per time can have this lock)
        /// </summary>
        public void EnterReserved(string collectionName)
        {
            ENSURE(_transaction.IsReadLockHeld || _transaction.IsWriteLockHeld, "Use EnterTransaction() before EnterReserved(name)");

            // checks if engine was open in readonly mode
            if (_readonly) throw new LiteException(0, "This operation are not support because engine was open in reaodnly mode");

            // if thread are in full reserved, don't try lock
            if (_reserved.IsWriteLockHeld) return;

            // reserved locker in read lock (if not already reserved in this thread be another snapshot)
            if (_reserved.IsReadLockHeld == false && _reserved.TryEnterReadLock(_timeout) == false) throw LiteException.LockTimeout("reserved", collectionName, _timeout);

            // get collection locker from dictionary (or create new if doesnt exists)
            var collection = _collections.GetOrAdd(collectionName, (s) => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));

            // try enter in reserved lock in collection
            if (collection.TryEnterUpgradeableReadLock(_timeout) == false)
            {
                // if get timeout, release first reserved lock
                _reserved.ExitReadLock();
                throw LiteException.LockTimeout("reserved", collectionName, _timeout);
            }
        }

        /// <summary>
        /// Exit collection reserved lock
        /// </summary>
        public void ExitReserved(string collectionName)
        {
            // if thread are in full reserved just exit
            if (_reserved.IsWriteLockHeld) return;

            if (_collections.TryGetValue(collectionName, out var collection) == false) throw LiteException.CollectionLockerNotFound(collectionName);

            collection.ExitUpgradeableReadLock();

            // in global reserved case, you can have same thread tring read-lock twice on different snapshot - exit once
            if (_reserved.IsReadLockHeld)
            {
                _reserved.ExitReadLock();
            }
        }

        /// <summary>
        /// Enter all database in reserved lock. Wait for all reader/writers. 
        /// If exclusive = false, new readers can read but no writers can write. If exclusive = true, no new readers/writers
        /// </summary>
        public void EnterReserved(bool exclusive)
        {
            // checks if engine was open in readonly mode
            if (_readonly) throw new LiteException(0, "This operation are not support because engine was open in reaodnly mode");

            // wait finish all transactions before enter in reserved mode
            if (_transaction.TryEnterWriteLock(_timeout) == false) throw LiteException.LockTimeout("reserved", _timeout);

            ENSURE(_transaction.RecursiveReadCount == 0, "must have no other transaction here");

            try
            {
                // reserved locker in write lock
                if (_reserved.TryEnterWriteLock(_timeout) == false)
                {
                    // exit transaction write lock
                    _transaction.ExitWriteLock();

                    throw LiteException.LockTimeout("reserved", _timeout);
                }
            }
            finally
            {
                if (exclusive == false)
                {
                    // exit exclusive and allow new readers
                    _transaction.ExitWriteLock();
                }
            }
        }


        /// <summary>
        /// Try enter in exclusive mode (same as ReservedMode) - if not possible, just exit with false (do not wait and no exceptions)
        /// Use ExitReserved(true) to exit
        /// </summary>
        public bool TryEnterExclusive()
        {
            // if is readonly or already in a transaction
            if (_readonly || _transaction.IsReadLockHeld) return false;

            // wait finish all transactions before enter in reserved mode
            if (_transaction.TryEnterWriteLock(10) == false) return false;

            ENSURE(_transaction.RecursiveReadCount == 0, "must have no other transaction here");

            // reserved locker in write lock
            if (_reserved.TryEnterWriteLock(10) == false)
            {
                // exit transaction write lock
                _transaction.ExitWriteLock();

                return false;
            }

            return true;
        }

        /// <summary>
        /// Exit reserved/exclusive lock
        /// </summary>
        public void ExitReserved(bool exclusive)
        {
            if (_reserved.IsWriteLockHeld)
            {
                _reserved.ExitWriteLock();
            }

            // if in reserved exclusive - unlock transactions
            if (exclusive == true)
            {
                _transaction.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            this.SafeDispose(_transaction);
            this.SafeDispose(_reserved);

            foreach(var collections in _collections.Values)
            {
                this.SafeDispose(collections);
            }
        }

        /// <summary>
        /// Dispose class testing for lock synchronization
        /// </summary>
        private void SafeDispose(IDisposable obj)
        {
            try
            {
                obj.Dispose();
            }
            catch (SynchronizationLockException)
            {
            }
        }
    }
}