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

        public LockService(IDiskService disk, TimeSpan timeout, Logger log)
        {
            _disk = disk;
            _log = log;
            _timeout = timeout;
            _state = LockState.Unlocked;
        }

        /// <summary>
        /// Try enter in shared lock (read)
        /// </summary>
        public LockControl Shared()
        {
            lock(_disk)
            {
                if (_state != LockState.Unlocked)
                {
                    return new LockControl(false, null);
                }

                _log.Write(Logger.DISK, "enter in shared lock mode");

                _disk.Lock(LockState.Shared);

                _state = LockState.Shared;
                _shared = true;

                return new LockControl(true, () =>
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
        public LockControl Reserved()
        {
            lock(_disk)
            {
                if (_state == LockState.Reserved)
                {
                    return new LockControl(false, null);
                }

                _log.Write(Logger.DISK, "enter in reserved lock mode");

                _disk.Lock(LockState.Reserved);

                _state = LockState.Reserved;

                // is new lock only when not came from a shared lock
                return new LockControl(!_shared, () =>
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

                // is not a new lock because always came from a reserved lock
                return new LockControl(false, () =>
                {
                    _log.Write(Logger.DISK, "exit in exclusive lock mode");
                    _state = LockState.Reserved;
                    _disk.Unlock(LockState.Exclusive);

                    // if in a shared lock, lock shared again (still reserved lock)
                    if (_shared)
                    {
                        _disk.Lock(LockState.Shared);
                    }
                });
            }
        }
    }
}