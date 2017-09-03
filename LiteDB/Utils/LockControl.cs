using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A class to control locking disposal. Can be a "new lock" - when a lock is not not coming from other lock state
    /// </summary>
    public class LockControl : IDisposable
    {
        private Action _dispose;

        /// <summary>
        /// Indicate that cache was clear becase has changes on file
        /// </summary>
        public bool Changed { get; private set; }

        internal LockControl(bool changed, Action dispose)
        {
            this.Changed = changed;
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_dispose != null) _dispose();
        }
    }
}