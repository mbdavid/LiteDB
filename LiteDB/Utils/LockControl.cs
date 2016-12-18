using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A class to control dispose locking. Can be a "new lock" - when a lock are not comming form other lock state
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
            if(_dispose != null) _dispose();
        }
    }
}