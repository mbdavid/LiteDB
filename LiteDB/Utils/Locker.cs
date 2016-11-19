using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A locker class that encapsulate ReaderWriterLockSlim instance
    /// </summary>
    internal class Locker
    {
        private ReaderWriterLockSlim _locker;
        private TimeSpan _timeout;

        public Locker(TimeSpan timeout)
        {
            _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _timeout = timeout;
        }

        /// <summary>
        /// Start new shared read lock control using timeout
        /// </summary>
        public LockControl Read()
        {
            // if current thread are in read mode, do nothing
            if(_locker.IsReadLockHeld || _locker.IsWriteLockHeld) return new LockControl(null);

            // try enter in read mode
            _locker.TryEnterReadLock(_timeout);

            // when dispose, close read mode
            return new LockControl(() => _locker.ExitReadLock());
        }

        /// <summary>
        /// Start new exclusive write lock control using timeout
        /// </summary>
        public LockControl Write()
        {
            // if current thread is already in write mode, do nothing
            if (_locker.IsWriteLockHeld) return new LockControl(null);

            // if current thread is in read mode, exit read mode first
            if (_locker.IsReadLockHeld)
            {
                _locker.ExitReadLock();
                _locker.TryEnterWriteLock(_timeout);

                // when dispose write mode, enter again in read mode
                return new LockControl(() =>
                {
                    _locker.ExitWriteLock();
                    _locker.TryEnterReadLock(_timeout);
                });
            }

            // try enter in write mode
            _locker.TryEnterWriteLock(_timeout);

            // and release when dispose
            return new LockControl(() => _locker.ExitWriteLock());
        }
    }

    /// <summary>
    /// Locker class that implement IDisposable to realease lock mode
    /// </summary>
    public class LockControl : IDisposable
    {
        private Action _dispose;

        internal LockControl(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if(_dispose != null) _dispose();
        }
    }
}