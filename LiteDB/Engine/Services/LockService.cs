using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement a locker service locking datafile to shared/reserved and exclusive mode
    /// Implement both Thread lock and Process lock
    /// Shared -> Reserved -> Exclusive => !Reserved => !Shared
    /// Reserved -> Exclusive => !Reserved
    /// [Thread Safe]
    /// </summary>
    internal class LockService
    {
        private TimeSpan _timeout;
        private IDiskService _disk;
        private Logger _log;
        private LockState _state;
        private bool _shared = false;
        private ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public LockService(IDiskService disk, TimeSpan timeout, Logger log)
        {
            _disk = disk;
            _log = log;
            _timeout = timeout;
            _state = LockState.Unlocked;
        }

        #region Process lock control

        public LockState State { get { return _state; } }

        /// <summary>
        /// Try enter in shared lock (read) - Call action if request a new lock
        /// </summary>
        public LockControl Shared(Action actionIfNewLock)
        {
            lock(_disk)
            {
                if (_state != LockState.Unlocked)
                {
                    return new LockControl(null);
                }

                _log.Write(Logger.DISK, "enter in shared lock mode");

                _disk.Lock(LockState.Shared);

                _state = LockState.Shared;
                _shared = true;

                actionIfNewLock();

                return new LockControl(() =>
                {
                    _log.Write(Logger.DISK, "exit shared lock mode");

                    _shared = false;
                    _state = LockState.Unlocked;

                    _disk.Unlock(LockState.Shared);
                });
            }
        }

        /// <summary>
        /// Try enter in reserved mode (read - single reserved)
        /// </summary>
        public LockControl Reserved(Action actionIfNewLock)
        {
            lock(_disk)
            {
                if (_state == LockState.Reserved)
                {
                    return new LockControl(null);
                }

                _log.Write(Logger.DISK, "enter in reserved lock mode");

                _disk.Lock(LockState.Reserved);

                _state = LockState.Reserved;

                // can be a new lock, calls action to notifify
                if(!_shared)
                {
                    actionIfNewLock();
                }

                // is new lock only when not came from a shared lock
                return new LockControl(() =>
                {
                    _log.Write(Logger.DISK, "exit in reserved lock mode");

                    _state = _shared ? LockState.Shared : LockState.Unlocked;
                    _disk.Unlock(LockState.Reserved);
                });
            }
        }

        /// <summary>
        /// Try enter in exclusive mode (single write)
        /// </summary>
        public LockControl Exclusive()
        {
            lock(_disk)
            {
                // has a shared lock? unlock first (will keep reserved lock)
                if(_shared)
                {
                    _disk.Unlock(LockState.Shared);
                }

                _log.Write(Logger.DISK, "enter in exclusive lock mode");

                _disk.Lock(LockState.Exclusive);
                _state = LockState.Exclusive;

                return new LockControl(() =>
                {
                    _log.Write(Logger.DISK, "exit in exclusive lock mode");
                    _state = LockState.Reserved;
                    _disk.Unlock(LockState.Exclusive);

                    // if was in a shared lock before exclusive lock, back to shared again (still reserved lock)
                    if (_shared)
                    {
                        _disk.Lock(LockState.Shared);
                    }
                });
            }
        }

        #endregion

        #region Thread lock control

        /// <summary>
        /// Start new shared read lock control using timeout
        /// </summary>
        public LockControl Read()
        {
            // if current thread are in read mode, do nothing
            if (_locker.IsReadLockHeld || _locker.IsWriteLockHeld) return new LockControl(null);

            // try enter in read mode
            _locker.TryEnterReadLock(_timeout);

            // when dispose, close read mode
            return new LockControl(_locker.ExitReadLock);
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
            return new LockControl(_locker.ExitWriteLock);
        }

        #endregion
    }
}