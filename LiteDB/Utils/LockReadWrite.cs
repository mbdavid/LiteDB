using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// A class to control locking disposal states
    /// </summary>
    public class LockReadWrite : IDisposable
    {
        public ReaderWriterLockSlim Reader { get; private set; }
        public ReaderWriterLockSlim Header { get; set; } = null;
        public List<ReaderWriterLockSlim> Collections { get; set; } = new List<ReaderWriterLockSlim>();

        private Logger _log;

        internal LockReadWrite(ReaderWriterLockSlim reader, Logger log)
        {
            this.Reader = reader;
            _log = log;
        }

        /// <summary>
        /// When dipose, exit all lock states
        /// </summary>
        public void Dispose()
        {
#if DEBUG
            _log.Write(Logger.LOCK, "exiting read lock mode on thread {0}", Thread.CurrentThread.ManagedThreadId);
#endif

            this.Reader.ExitReadLock();

            if (this.Header != null)
            {
#if DEBUG
                _log.Write(Logger.LOCK, "exiting header write lock mode on thread {0}", Thread.CurrentThread.ManagedThreadId);
#endif

                this.Header.ExitWriteLock();
            }

            foreach(var col in this.Collections)
            {
#if DEBUG
                _log.Write(Logger.LOCK, "exiting collection write lock mode in collection ?? on thread {0}", Thread.CurrentThread.ManagedThreadId);
#endif
                col.ExitWriteLock();
            }
        }
    }
}