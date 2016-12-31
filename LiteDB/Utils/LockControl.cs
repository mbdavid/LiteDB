using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A class to control locking disposal. Can be a "new lock" - when a lock is not not coming from other lock state
    /// </summary>
    public class LockControl : IDisposable
    {
        // dispose based on
        // https://lostechies.com/chrispatterson/2012/11/29/idisposable-done-right/

        private Action _dispose;
        private bool _disposed;

        internal LockControl(Action dispose)
        {
            _dispose = dispose;
        }

        ~LockControl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
            }

            if (_dispose != null) _dispose();

            _disposed = true;
        }
    }
}