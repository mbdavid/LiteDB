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

        #region Initialize

        public Locker(TimeSpan timeout)
        {
            _locker = new ReaderWriterLockSlim();
            _timeout = timeout;
        }

        /// <summary>
        /// Start new shared read lock control using timeout
        /// </summary>
        public LockControl Read()
        {
            return new LockControl(_locker, _timeout, false);
        }

        /// <summary>
        /// Start new exclusive write lock control using timeout
        /// </summary>
        public LockControl Write()
        {
            return new LockControl(_locker, _timeout, true);
        }

        #endregion

        /// <summary>
        /// Inner class that implement IDisposable to control lock 
        /// </summary>
        public class LockControl : IDisposable
        {
            private ReaderWriterLockSlim _locker;
            private bool _write = false;

            public LockControl(ReaderWriterLockSlim locker, TimeSpan timeout, bool write)
            {
                _locker = locker;
                _write = write;

                if (write)
                {
                    _locker.TryEnterWriteLock(timeout);
                }
                else
                {
                    _locker.TryEnterReadLock(timeout);
                }
            }

            public void Dispose()
            {
                if (_write)
                {
                    _locker.ExitWriteLock();
                }
                else
                {
                    _locker.ExitReadLock();
                }
            }
        }
    }
}