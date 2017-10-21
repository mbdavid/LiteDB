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
#if HAVE_THREAD_ID
                return Thread.CurrentThread.ManagedThreadId;
#else
                return System.Threading.Tasks.Task.CurrentId ?? 0;
#endif
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
                return new LockControl(false, () => { });
            }

            // try enter in read mode
            if (!_thread.TryEnterReadLock(_timeout))
            {
                throw LiteException.LockTimeout(_timeout);
            }

            _log.Write(Logger.LOCK, "entered in read lock mode in thread #{0}", this.ThreadId);

            // lock disk in shared mode
            var position = _disk.Lock(LockState.Read, _timeout);

            var changed = this.DetectDatabaseChanges();

            return new LockControl(changed, () =>
            {
                // exit disk lock mode
                _disk.Unlock(LockState.Read, position);

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
                return new LockControl(false, () => { });
            }

            // let's test if is not in read lock
            if (_thread.IsReadLockHeld) throw new NotSupportedException("Not support Write lock inside a Read lock");

            // try enter in write mode (thread)
            if (!_thread.TryEnterWriteLock(_timeout))
            {
                throw LiteException.LockTimeout(_timeout);
            }

            _log.Write(Logger.LOCK, "entered in write lock mode in thread #{0}", this.ThreadId);

            // try enter in exclusive mode in disk
            var position = _disk.Lock(LockState.Write, _timeout);

            // call avoid dirty only if not came from a shared mode
            var changed = this.DetectDatabaseChanges();

            return new LockControl(changed, () =>
            {
                // release disk write
                _disk.Unlock(LockState.Write, position);

                // release thread write
                _thread.ExitWriteLock();

                _log.Write(Logger.LOCK, "exited write lock mode in thread #{0}", this.ThreadId);
            });
        }

        #endregion

        /// <summary>
        /// Test if cache still valid (if datafile was changed by another process reset cache)
        /// Returns true if file was changed
        /// [Thread Safe]
        /// </summary>
        private bool DetectDatabaseChanges()
        {
            // if disk are exclusive don't need check dirty read
            if (_disk.IsExclusive) return false;

            // empty cache? just exit
            if (_cache.CleanUsed == 0) return false;

            _log.Write(Logger.CACHE, "checking disk to detect database changes from another process");

            // get ChangeID from cache
            var header = _cache.GetPage(0) as HeaderPage;
            var changeID = header == null ? 0 : header.ChangeID;

            // and get header from disk
            var disk = BasePage.ReadPage(_disk.ReadPage(0)) as HeaderPage;

            // if disk header are in recovery mode, throw exception to datafile re-open and recovery pages
            if (disk.Recovery)
            {
                _log.Write(Logger.ERROR, "datafile in recovery mode, need re-open database");

                throw LiteException.NeedRecover();
            }

            // if header change, clear cache and add new header to cache
            if (disk.ChangeID != changeID)
            {
                _log.Write(Logger.CACHE, "file changed from another process, cleaning all cache pages");

                _cache.ClearPages();
                _cache.AddPage(disk);
                return true;
            }

            return false;
        }
    }
}