using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement temporary disk access. Open datafile only when be used and delete when dispose. No journal, no sharing
    /// </summary>
    public class TempDiskService : IDiskService
    {
        /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        private FileStream _stream;
        private string _filename;

        #region Initialize/Dispose disk

        public TempDiskService()
        {
        }

        public void Initialize(Logger log, string password)
        {
            // datafile will be created only when used
        }

        private void InternalInitialize()
        {
            // create a temp filename in temp directory
            _filename = Path.Combine(Path.GetTempPath(), "litedb-sort-" + Guid.NewGuid().ToString("n").Substring(0, 6) + ".db");
            
            // create disk 
            _stream = this.CreateFileStream(_filename, System.IO.FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        }

        public virtual void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;

                // after release stream, delete datafile
                FileHelper.TryDelete(_filename);
            }
        }

        #endregion

        #region Read/Write

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public virtual byte[] ReadPage(uint pageID)
        {
            // if stream are not initialized but need header, create new header
            if(_stream == null && pageID == 0)
            {
                var header = new HeaderPage
                {
                    LastPageID = 1
                };

                return header.WritePage();
            }
            else if (_stream == null)
            {
                this.InternalInitialize();
            }

            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            // read bytes from data file
            _stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            return buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public virtual void WritePage(uint pageID, byte[] buffer)
        {
            if (_stream == null) this.InternalInitialize();

            var position = BasePage.GetSizeOfPages(pageID);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
        }

        /// <summary>
        /// Set datafile length
        /// </summary>
        public void SetLength(long fileSize)
        {
            if (_stream == null) this.InternalInitialize();

            // fileSize parameter tell me final size of data file - helpful to extend first datafile
            _stream.SetLength(fileSize);
        }

        /// <summary>
        /// Returns file length
        /// </summary>
        public long FileLength { get { return _stream?.Length ?? 0; } }

        #endregion

        #region Journal file

        /// <summary>
        /// No journal
        /// </summary>
        public bool IsJournalEnabled { get { return false; } }

        /// <summary>
        /// No journal
        /// </summary>
        public void WriteJournal(ICollection<byte[]> pages, uint lastPageID)
        {
        }

        /// <summary>
        /// No journal
        /// </summary>
        public IEnumerable<byte[]> ReadJournal(uint lastPageID)
        {
            yield break;
        }

        /// <summary>
        /// No journal
        /// </summary>
        public void ClearJournal(uint lastPageID)
        {
        }

        /// <summary>
        /// Flush data from memory to disk
        /// </summary>
        public void Flush()
        {
            if (_stream != null)
            {
                _stream.Flush();
            }
        }

        #endregion

        #region Lock / Unlock

        /// <summary>
        /// Indicate disk can be access by multiples processes or not
        /// </summary>
        public bool IsExclusive { get { return true; } }

        /// <summary>
        /// Exclusive - no lock
        /// </summary>
        public int Lock(LockState state, TimeSpan timeout)
        {
            return 0;
        }

        /// <summary>
        /// Exclusive - no lock
        /// </summary>
        public void Unlock(LockState state, int position)
        {
        }

        #endregion

        #region Create Stream

        /// <summary>
        /// Create a new filestream. Can be synced over async task (netstandard)
        /// </summary>
        private FileStream CreateFileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share)
        {
#if HAVE_SYNC_OVER_ASYNC
            // if (_options.Async) 
            {
                return System.Threading.Tasks.Task.Run(() => new FileStream(path, mode, access, share, BasePage.PAGE_SIZE))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
#endif
            return new FileStream(path, mode, access, share, 
                BasePage.PAGE_SIZE,
                System.IO.FileOptions.RandomAccess);
        }

        #endregion
    }
}