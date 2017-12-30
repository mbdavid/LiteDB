using System;
using System.Collections.Generic;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A class to control locking disposal states
    /// </summary>
    public class LockControl : IDisposable
    {
        public ReaderWriterLockSlim Reader { get; set; }
        public ReaderWriterLockSlim Header { get; set; } = null;
        public List<ReaderWriterLockSlim> Collections { get; set; } = new List<ReaderWriterLockSlim>();

        internal LockControl(ReaderWriterLockSlim reader)
        {
            this.Reader = reader;
        }

        /// <summary>
        /// When dipose, exit all lock states
        /// </summary>
        public void Dispose()
        {
            this.Reader.ExitReadLock();

            if (this.Header != null)
            {
                this.Header.ExitWriteLock();
            }

            foreach(var col in this.Collections)
            {
                col.ExitWriteLock();
            }
        }
    }
}