using System;
using System.Collections.Generic;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A class to control locking disposal states
    /// </summary>
    public class LockExclusive : IDisposable
    {
        private ReaderWriterLockSlim _exclusive;

        internal LockExclusive(ReaderWriterLockSlim exclusive)
        {
            _exclusive = exclusive;
        }

        /// <summary>
        /// When dipose, exit all lock states
        /// </summary>
        public void Dispose()
        {
            _exclusive.ExitWriteLock();
        }
    }
}