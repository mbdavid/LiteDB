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

        internal LockControl(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_dispose != null) _dispose();
        }
    }
}