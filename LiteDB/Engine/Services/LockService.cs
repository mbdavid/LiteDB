using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement simple lock service (multi-reader/single-writer [with no-reader])
    /// [Thread Safe]
    /// </summary>
    public class LockService
    {
        #region Properties + Ctor

        private TimeSpan _timeout;
        private IDiskService _disk;
        private CacheService _cache;
        private Logger _log;
        private LockState _state;
        private ReaderWriterLockSlim _thread = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        internal LockService(IDiskService disk, CacheService cache, TimeSpan timeout, Logger log)
        {
            _disk = disk;
            _cache = cache;
            _log = log;
            _timeout = timeout;
            _state = LockState.Unlocked;
        }

        /// <summary>
        /// Get current datafile lock state
        /// </summary>
        public LockState State { get { return _state; } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enter in Shared lock mode.
        /// </summary>
        public LockControl Read()
        {
            lock(_disk)
            {
                // if lock already in shared or exclusive, do nothing
                if (_state != LockState.Unlocked)
                {
                    return new LockControl(() => { });
                }

                // try enter in read mode
                if (!_thread.TryEnterReadLock(_timeout))
                {
                    throw LiteException.LockTimeout(_timeout);
                }

                // lock disk in shared mode
                _disk.Lock(LockState.Read, _timeout);

                _log.Write(Logger.LOCK, "entered in shared lock mode");

                this.DetectDatabaseChanges();

                _state = LockState.Read;

                return new LockControl(() =>
                {
                    // exit thread lock mode
                    _thread.ExitReadLock();

                    // exit disk lock mode
                    _disk.Unlock(LockState.Read);

                    _log.Write(Logger.LOCK, "exited shared lock mode");

                    _state = LockState.Unlocked;
                });
            }
        }

        /// <summary>
        /// Enter in Exclusive lock mode
        /// </summary>
        public LockControl Write()
        {
            lock (_disk)
            {
                // if already in exclusive, do nothing
                if (_state == LockState.Write)
                {
                    return new LockControl(() => { });
                }

                // test if came from a shared lock (to restore after unlock)
                var read = _state == LockState.Read;

                // if came, need exit read lock
                if (read) _thread.ExitReadLock();

                // try enter in write mode (thread)
                if (!_thread.TryEnterWriteLock(_timeout))
                {
                    throw LiteException.LockTimeout(_timeout);
                }

                // try enter in exclusive mode in disk
                _disk.Lock(LockState.Write, _timeout);

                _log.Write(Logger.LOCK, "entered in exclusive lock mode");

                // call avoid dirty only if not came from a shared mode
                if (!read)
                {
                    this.DetectDatabaseChanges();
                }

                _state = LockState.Write;

                return new LockControl(() =>
                {
                    // release thread write
                    _thread.ExitWriteLock();

                    // 
                    if (read)
                    {
                        _thread.TryEnterReadLock(_timeout);
                        _state = LockState.Read;
                    }

                    // release disk write
                    _disk.Unlock(LockState.Write);

                    if (!read)
                    {
                        _state = LockState.Unlocked;
                    }
                });
            }
        }

        #endregion

        /// <summary>
        /// Test if cache still valid (if datafile was changed by another process reset cache)
        /// [Thread Safe]
        /// </summary>
        private void DetectDatabaseChanges()
        {
            // if disk are exclusive don't need check dirty read
            if (_disk.IsExclusive) return;

            _log.Write(Logger.CACHE, "checking disk to avoid dirty read");

            // empty cache? just exit
            if (_cache.CleanUsed == 0) return;

            // get ChangeID from cache
            var header = _cache.GetPage(0) as HeaderPage;
            var changeID = header == null ? 0 : header.ChangeID;

            // and get header from disk
            var disk = BasePage.ReadPage(_disk.ReadPage(0)) as HeaderPage;

            // if disk header are in recovery mode, throw exception to datafile re-open and recovery pages
            if (disk.Recovery)
            {
                throw LiteException.NeedRecover();
            }

            // if header change, clear cache and add new header to cache
            if (disk.ChangeID != changeID)
            {
                _log.Write(Logger.CACHE, "file changed from another process, cleaning all cache pages");

                _cache.ClearPages();
                _cache.AddPage(disk);
            }
        }
    }
}