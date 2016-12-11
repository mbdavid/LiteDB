using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A locker class that encapsulate ReaderWriterLockSlim instance
    /// </summary>
    internal class LockFile
    {
        private TimeSpan _timeout;
        private string _filename;

        public LockFile(TimeSpan timeout, string filename)
        {
            _timeout = timeout;
            _filename = filename;
        }

        public void TryEnterSharedLock()
        {
        }

        public void TryEnterReservedLock()
        {
        }

        public void TryEnterExclusiveLock()
        {
        }

        public void ExitSharedLock()
        {
        }

        public void ExitReservedLock()
        {
        }

        public void ExitExclusiveLock()
        {
        }

        private void OpenFile()
        {
        }

        private void CloseFile()
        {
        }
    }
}