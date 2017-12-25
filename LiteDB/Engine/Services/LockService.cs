using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement simple lock service (multi-reader/single-writer [with no-reader])
    /// Use ReaderWriterLockSlim for thread lock and FileStream.Lock for file (inside disk impl)
    /// [Thread Safe]
    /// </summary>
    public class LockService
    {
        #region Properties + Ctor

        private TimeSpan _timeout;
        private IDiskService _disk;
        private CacheService _cache;
        private Logger _log;
        private ReaderWriterLockSlim _thread = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        internal LockService(IDiskService disk, CacheService cache, TimeSpan timeout, Logger log)
        {
            _disk = disk;
            _cache = cache;
            _log = log;
            _timeout = timeout;
        }

        /// <summary>
        /// Get current datafile lock state defined by thread only (do not check if datafile is locked)
        /// </summary>
        public LockState ThreadState
        {
            get
            {
                return _thread.IsWriteLockHeld ? LockState.Write :
                    _thread.CurrentReadCount > 0 ? LockState.Read : LockState.Unlocked;
            }
        }

        /// <summary>
        /// Get current thread id
        /// </summary>
        public int ThreadId
        {
            get
            {
                return System.Threading.Tasks.Task.CurrentId ?? 0;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enter in Shared lock mode.
        /// </summary>
        public LockControl Read()
        {
            // if read or write
            if (_thread.IsReadLockHeld || _thread.IsWriteLockHeld)
            {
                return new LockControl(() => { });
            }

            // try enter in read mode
            if (!_thread.TryEnterReadLock(_timeout))
            {
                throw LiteException.LockTimeout(_timeout);
            }

            _log.Write(Logger.LOCK, "entered in read lock mode in thread #{0}", this.ThreadId);

            return new LockControl(() =>
            {
                // exit thread lock mode
                _thread.ExitReadLock();

                _log.Write(Logger.LOCK, "exited read lock mode in thread #{0}", this.ThreadId);
            });
        }

        /// <summary>
        /// Enter in Exclusive lock mode
        /// </summary>
        public LockControl Write()
        {
            // if already in exclusive, do nothing
            if (_thread.IsWriteLockHeld)
            {
                return new LockControl(() => { });
            }

            // let's test if is not in read lock
            if (_thread.IsReadLockHeld) throw new NotSupportedException("Not support Write lock inside a Read lock");

            // try enter in write mode (thread)
            if (!_thread.TryEnterWriteLock(_timeout))
            {
                throw LiteException.LockTimeout(_timeout);
            }

            _log.Write(Logger.LOCK, "entered in write lock mode in thread #{0}", this.ThreadId);

            return new LockControl(() =>
            {
                // release thread write
                _thread.ExitWriteLock();

                _log.Write(Logger.LOCK, "exited write lock mode in thread #{0}", this.ThreadId);
            });
        }

        #endregion
    }
}