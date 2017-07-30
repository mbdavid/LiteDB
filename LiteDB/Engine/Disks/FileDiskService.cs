using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

namespace LiteDB
{
    /// <summary>
    /// Implement NTFS File disk
    /// </summary>
    public class FileDiskService : IDiskService
    {
        /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        /// <summary>
        /// Map lock positions
        /// </summary>
        internal const int LOCK_POSITION = BasePage.PAGE_SIZE; // use second page
        internal const int LOCK_LENGTH = 1000;

        private FileStream _stream;
        private string _filename;

        private Logger _log; // will be initialize in "Initialize()"
        private FileOptions _options;

#if NET35
        private int _lockReadPosition = 0;
        private Random _lockReadRandom = new Random();
#endif

        #region Initialize/Dispose disk

        public FileDiskService(string filename, bool journal = true)
            : this(filename, new FileOptions { Journal = journal })
        {
        }

        public FileDiskService(string filename, FileOptions options)
        {
            // simple validations
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException("filename");
            if (options.InitialSize > options.LimitSize) throw new ArgumentException("limit size less than initial size");

            // setting class variables
            _filename = filename;
            _options = options;
        }

        public void Initialize(Logger log, string password)
        {
            // get log instance to disk
            _log = log;

            // if is read only, journal must be disabled
            if (_options.FileMode == FileMode.ReadOnly) _options.Journal = false;

            // open/create file using read only/exclusive options
            _stream = CreateFileStream(_filename,
                _options.FileMode == FileMode.ReadOnly ? System.IO.FileMode.Open : System.IO.FileMode.OpenOrCreate,
                _options.FileMode == FileMode.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
                _options.FileMode == FileMode.Exclusive ? FileShare.None : FileShare.ReadWrite);

            // if file is new, initialize
            if (_stream.Length == 0)
            {
                _log.Write(Logger.DISK, "initialize new datafile");

                // set datafile initial size
                _stream.SetLength(_options.InitialSize);

                // create datafile
                LiteEngine.CreateDatabase(_stream, password);
            }
        }

        public virtual void Dispose()
        {
            if (_stream != null)
            {
                _log.Write(Logger.DISK, "close datafile '{0}'", Path.GetFileName(_filename));
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion

        #region Read/Write

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public virtual byte[] ReadPage(uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            // read bytes from data file
            _stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            _log.Write(Logger.DISK, "read page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            return buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public virtual void WritePage(uint pageID, byte[] buffer)
        {
            var position = BasePage.GetSizeOfPages(pageID);

            _log.Write(Logger.DISK, "write page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

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
            // checks if new fileSize will exceed limit size
            if (fileSize > _options.LimitSize) throw LiteException.FileSizeExceeded(_options.LimitSize);

            // fileSize parameter tell me final size of data file - helpful to extend first datafile
            _stream.SetLength(fileSize);
        }

        /// <summary>
        /// Returns file length
        /// </summary>
        public long FileLength { get { return _stream.Length; } }

        #endregion

        #region Journal file

        /// <summary>
        /// Write original bytes page in a journal file (in sequence) - if journal not exists, create.
        /// </summary>
        public void WriteJournal(ICollection<byte[]> pages, uint lastPageID)
        {
            // write journal only if enabled
            if (_options.Journal == false) return;

            var size = BasePage.GetSizeOfPages(lastPageID + 1) +
                BasePage.GetSizeOfPages(pages.Count);

            _log.Write(Logger.JOURNAL, "extend datafile to journal pages: {0}", size);

            // set journal file length before write
            _stream.SetLength(size);

            // go to initial file position (after lastPageID)
            _stream.Seek(BasePage.GetSizeOfPages(lastPageID + 1), SeekOrigin.Begin);

            foreach(var buffer in pages)
            {
                // read pageID and pageType from buffer
                var pageID = BitConverter.ToUInt32(buffer, 0);
                var pageType = (PageType)buffer[PAGE_TYPE_POSITION];

                _log.Write(Logger.JOURNAL, "write page #{0:0000} :: {1}", pageID, pageType);

                // write page bytes
                _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
            }

            _log.Write(Logger.JOURNAL, "flush journal to disk", size);

            // ensure all data are persisted in disk
            this.Flush();
        }

        /// <summary>
        /// Read journal file returning IEnumerable of pages
        /// </summary>
        public IEnumerable<byte[]> ReadJournal(uint lastPageID)
        {
            // if journal are not enabled, just return empty result
            if (_options.Journal == false) yield break;

            // position stream at begin journal area
            _stream.Seek(BasePage.GetSizeOfPages(lastPageID + 1), SeekOrigin.Begin);

            var buffer = new byte[BasePage.PAGE_SIZE];

            while (_stream.Position <= _stream.Length)
            {
                // read page bytes from journal file
                _stream.Read(buffer, 0, BasePage.PAGE_SIZE);

                yield return buffer;
            }
        }

        /// <summary>
        /// Shrink datafile to crop journal area
        /// </summary>
        public void ClearJournal(uint lastPageID)
        {
            _log.Write(Logger.JOURNAL, "shrink datafile to remove journal area");

            this.SetLength(BasePage.GetSizeOfPages(lastPageID + 1));
        }

        /// <summary>
        /// Flush data from memory to disk
        /// </summary>
        public void Flush()
        {
            _log.Write(Logger.DISK, "flush data from memory to disk");

#if NET35
            _stream.Flush();
#endif
#if !NET35
            _stream.Flush(true);
#endif
        }

        #endregion

        #region Lock / Unlock

        /// <summary>
        /// Indicate disk can be access by multiples processes or not
        /// </summary>
        public bool IsExclusive { get { return _options.FileMode == FileMode.Exclusive; } }

        /// <summary>
        /// Implement datafile lock/unlock
        /// </summary>
        public void Lock(LockState state, TimeSpan timeout)
        {
#if NET35
            // only shared mode lock datafile
            if (_options.FileMode != FileMode.Shared) return;

            var position = state == LockState.Read ? _lockReadPosition = _lockReadRandom.Next(LOCK_POSITION, LOCK_POSITION + LOCK_LENGTH) : LOCK_POSITION;
            var length = state == LockState.Read ? 1 : LOCK_LENGTH;

            _log.Write(Logger.LOCK, "locking file in {0} mode (position: {1}, length: {2})", state.ToString().ToLower(), position, length);

            if (state == LockState.Write && _lockReadPosition > 0)
            {
                var beforeLength = _lockReadPosition - LOCK_POSITION;
                var afterLength = LOCK_LENGTH - beforeLength - 1;

                _stream.TryLock(LOCK_POSITION, beforeLength, timeout);
                _stream.TryLock(_lockReadPosition + 1, afterLength, timeout);
            }
            else
            {
                _stream.TryLock(position, length, timeout);
            }
#endif
        }

        /// <summary>
        /// Unlock datafile based on state
        /// </summary>
        public void Unlock(LockState state)
        {
#if NET35
            // only shared mode lock datafile
            if (_options.FileMode != FileMode.Shared) return;

            var position = state == LockState.Read ? _lockReadPosition : LOCK_POSITION;
            var length = state == LockState.Read ? 1 : LOCK_LENGTH;

            _log.Write(Logger.LOCK, "unlocking file in {0} mode (position: {1}, length: {2})", state.ToString().ToLower(), position, length);

            // if unlock exclusive but contains position of shared lock, keep shared
            if (state == LockState.Write && _lockReadPosition != 0)
            {
                var beforeLength = _lockReadPosition - LOCK_POSITION;
                var afterLength = LOCK_LENGTH - beforeLength - 1;

                _stream.TryUnlock(LOCK_POSITION, beforeLength);
                _stream.TryUnlock(_lockReadPosition + 1, afterLength);
            }
            else
            {
                _stream.TryUnlock(position, length);

                _lockReadPosition = 0;
            }
#endif
        }

        #endregion

        #region Create Stream

        /// <summary>
        /// Create a new filestream. Can be synced over async task (netstandard)
        /// </summary>
        private FileStream CreateFileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share)
        {
#if !NET35
            if (_options.Async)
            {
                return this.SyncOverAsync(() => new FileStream(path, mode, access, share, BasePage.PAGE_SIZE));
            }
#endif
            return new FileStream(path, mode, access, share, 
                BasePage.PAGE_SIZE); 
                // System.IO.FileOptions.WriteThrough | System.IO.FileOptions.SequentialScan);
        }

#if !NET35
        private T SyncOverAsync<T>(Func<T> f)
        {
            return Task.Run<T>(f).ConfigureAwait(false).GetAwaiter().GetResult();
        }
#endif

        #endregion
    }
}