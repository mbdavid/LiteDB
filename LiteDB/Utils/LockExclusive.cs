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
        private Logger _log;

        internal LockExclusive(ReaderWriterLockSlim exclusive, Logger log)
        {
            _exclusive = exclusive;
            _log = log;
        }

        /// <summary>
        /// When dipose, exit all lock states
        /// </summary>
        public void Dispose()
        {
            _log.LockExit(_exclusive);

            _exclusive.ExitWriteLock();
        }
    }
}