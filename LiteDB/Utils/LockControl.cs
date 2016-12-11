using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Locker class that implement IDisposable to realease lock mode
    /// </summary>
    public class LockControl : IDisposable
    {
        private Action _dispose;

        /// <summary>
        /// When is new lock, must check if datafile change
        /// </summary>
        public bool IsNewLock { get; private set; }

        internal LockControl(bool newLock, Action dispose)
        {
            _dispose = dispose;

            this.IsNewLock = newLock;
        }

        public void Dispose()
        {
            if(_dispose != null) _dispose();
        }
    }
}