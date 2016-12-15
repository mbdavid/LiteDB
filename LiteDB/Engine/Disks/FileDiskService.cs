using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

        private FileStream _stream;
        private string _filename;

        private FileStream _journal;
        private string _journalFilename;

        private Logger _log; // will be initialize in "Initialize()"
        private FileOptions _options;

        /// <summary>
        /// Lock position/length of all lock state.
        /// </summary>
        private const int LOCK_SHARED_LENGTH = 1000;
        private const int LOCK_POSITION = BasePage.PAGE_SIZE; // use second page

        private LockPosition _lockShared = LockPosition.Empty; // will be random each call
        private LockPosition _lockReserved = new LockPosition(LOCK_POSITION + LOCK_SHARED_LENGTH + 1, 1); // reserved at end position
        private LockPosition _lockExclusive = new LockPosition(LOCK_POSITION, LOCK_SHARED_LENGTH); // all shared range

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

            // journal filename
            _journalFilename = FileHelper.GetTempFile(_filename, "-journal", false);
        }

        public void Initialize(Logger log, string password)
        {
            // get log instance to disk
            _log = log;

            // if is readonly, journal must be disabled
            if (_options.FileMode == FileMode.ReadOnly) _options.Journal = false;

            // open/create file using readonly/exclusive options
            _stream = new FileStream(_filename,
                _options.FileMode == FileMode.ReadOnly ? System.IO.FileMode.Open : System.IO.FileMode.OpenOrCreate,
                _options.FileMode == FileMode.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
                _options.FileMode == FileMode.Exclusive ? FileShare.None : FileShare.ReadWrite,
                BasePage.PAGE_SIZE);

            // if file is new, initialize
            if (_stream.Length == 0)
            {
                _log.Write(Logger.DISK, "initialize new datafile");

                // set datafile initial size
                _stream.SetLength(_options.InitialSize);

                // create a new header page in bytes (keep second page empty)
                var header = new HeaderPage() { LastPageID = 1 };

                if(password != null)
                {
                    _log.Write(Logger.DISK, "datafile encrypted");

                    header.Password = AesEncryption.HashSHA1(password);
                    header.Salt = AesEncryption.Salt();
                }

                // write bytes on page
                this.WritePage(0, header.WritePage());

                // write second page empty just to use as lock control
                this.WritePage(1, new byte[BasePage.PAGE_SIZE]);
            }
        }

        public virtual void Dispose()
        {
            if (_journal != null)
            {
                _log.Write(Logger.DISK, "close journal file '{0}'", Path.GetFileName(_journalFilename));
                _journal.Dispose();
                _journal = null;
                FileHelper.TryDelete(_journalFilename);
            }
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
        /// Returns if journal is enabled
        /// </summary>
        public bool IsJournalEnabled { get { return _options.Journal; } }

        /// <summary>
        /// Write original bytes page in a journal file (in sequence) - if journal not exists, create.
        /// </summary>
        public void WriteJournal(ICollection<byte[]> pages)
        {
            if (_journal == null)
            {
                // open or create datafile if not exists
                _journal = new FileStream(_journalFilename,
                    System.IO.FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite,
                    BasePage.PAGE_SIZE);
            }

#if NET35
            // lock first byte
            _journal.Lock(0, 1);
#endif
            // go to initial file position
            _journal.Seek(0, SeekOrigin.Begin);

            // set journal file length before write
            _journal.SetLength(pages.Count);

            foreach(var buffer in pages)
            {
                // read pageID and pageType from buffer
                var pageID = BitConverter.ToUInt32(buffer, 0);
                var pageType = (PageType)buffer[PAGE_TYPE_POSITION];

                _log.Write(Logger.JOURNAL, "write page #{0:0000} :: {1}", pageID, pageType);

                // write page bytes
                _journal.Write(buffer, 0, BasePage.PAGE_SIZE);
            }
        }

        /// <summary>
        /// Read journal file returning IEnumerable of pages
        /// </summary>
        public IEnumerable<byte[]> ReadJournal()
        {
            // check if exists journal file (if opended)
            if (_journal == null)
            {
                try
                {
                    _journal = new FileStream(_journalFilename,
                        System.IO.FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite,
                        BasePage.PAGE_SIZE);

#if NET35
                    // lock journal file during reading
                    _journal.Lock(0, 1);
#endif
                }
                catch(FileNotFoundException)
                {
                    yield break;
                }
                catch(IOException ex)
                {
                    if (ex.IsLocked()) yield break;
                    throw;
                }
            }

            var buffer = new byte[BasePage.PAGE_SIZE];

            // seek to begin file before start
            _journal.Seek(0, SeekOrigin.Begin);

            while (_journal.Position < _journal.Length)
            {
                // read page bytes from journal file
                _journal.Read(buffer, 0, BasePage.PAGE_SIZE);

                yield return buffer;
            }

#if NET35
            // unlock journal file
            _journal.Unlock(0, 1);
#endif
        }

        /// <summary>
        /// Clear jounal file (set size to 0 length)
        /// </summary>
        public void ClearJournal()
        {
            if (_journal != null)
            {
                _log.Write(Logger.JOURNAL, "cleaning journal file");
                _journal.TryUnlock(0, 1);
                _journal.Seek(0, SeekOrigin.Begin);
                _journal.SetLength(0);
            }
        }

        #endregion

        #region Lock / Unlock

        /// <summary>
        /// Indicate disk can be access by multiples processes or not
        /// </summary>
        public bool IsExclusive { get { return _options.FileMode == FileMode.Exclusive; } }

        /// <summary>
        /// Implement datafile lock/unlock
        /// Lock use last LOCK_LENGTH bytes from header page lock control
        /// - first byte is to reserved lock
        /// - second to last is to many shared lock (random 1)
        /// - all bytes are locked in exclusive mode
        /// </summary>
        public void Lock(LockState state)
        {
            // only shared mode lock datafile
            if (_options.FileMode != FileMode.Shared) return;

            var pos = 
                state == LockState.Shared ? _lockShared = LockPosition.Random(LOCK_POSITION, LOCK_SHARED_LENGTH) :
                state == LockState.Reserved ? _lockReserved :
                state == LockState.Exclusive ? _lockExclusive : LockPosition.Empty;

            // try lock surface during timeout
            _stream.TryLock(pos.Position, pos.Length, _options.Timeout);
        }

        /// <summary>
        /// Unlock datafile based on state
        /// </summary>
        public void Unlock(LockState state)
        {
            // only shared mode lock datafile
            if (_options.FileMode != FileMode.Shared) return;

            var pos =
                state == LockState.Shared ? _lockShared :
                state == LockState.Reserved ? _lockReserved :
                state == LockState.Exclusive ? _lockExclusive : LockPosition.Empty;

            _stream.Unlock(pos.Position, pos.Length);
        }

        #endregion
    }
}