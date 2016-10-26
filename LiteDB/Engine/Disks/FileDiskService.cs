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

        private Stream _stream;
        private string _filename;

        private Stream _journal;
        private string _journalFilename;
        private HashSet<uint> _journalPages = new HashSet<uint>();

        private Logger _log; // will be initialize in "Initialize()"
        private FileOptions _options;

        #region Initialize disk

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
            _journalFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-journal" + Path.GetExtension(_filename));
        }

        public void Initialize(Logger log)
        {
            _log = log;

            // if file not exists, just create empty file
            if (!File.Exists(_filename))
            {
                // open file as create mode
                using (var stream = new FileStream(_filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, BasePage.PAGE_SIZE))
                {
                    _log.Write(Logger.DISK, "initialize new datafile");

                    // set datafile initial size
                    stream.SetLength(_options.InitialSize);

                    // create a new header page in bytes
                    var bytes = InitializeHeaderPage().WritePage();

                    // write bytes on page
                    stream.Write(bytes, 0, BasePage.PAGE_SIZE);
                }
            }
        }

        internal virtual HeaderPage InitializeHeaderPage()
        {
            return new HeaderPage();
        }

        public virtual void Open()
        {
            // try open file in exclusive mode
            TryExec(() =>
            {
                _stream = new FileStream(_filename, FileMode.Open, 
                    FileAccess.ReadWrite, FileShare.Read, 
                    BasePage.PAGE_SIZE);
            });
        }

        #endregion

        public virtual void Dispose()
        {
            if (_journal != null)
            {
                _log.Write(Logger.DISK, "close journal file '{0}'", Path.GetFileName(_journalFilename));
                _journal.Dispose();
                _journal = null;
                File.Delete(_journalFilename);
            }
            if (_stream != null)
            {
                _log.Write(Logger.DISK, "close datafile '{0}'", Path.GetFileName(_filename));
                _stream.Dispose();
                _stream = null;
            }
        }

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
        /// Returns if journal is enabled
        /// </summary>
        public bool IsJournalEnabled { get { return _options.Journal; } }

        #endregion

        #region Journal file

        /// <summary>
        /// Write original bytes page in a journal file (in sequence) - if journal not exists, create.
        /// </summary>
        public void WriteJournal(uint pageID, byte[] buffer)
        {
            // test if this page is not in journal file
            if (_journalPages.Contains(pageID)) return;

            // if no journal or no buffer, exit
            if (_options.Journal == false || buffer.Length == 0) return;

            // open journal file if not used yet
            if (_journal == null)
            {
                // open journal file in EXCLUSIVE mode
                _log.Write(Logger.JOURNAL, "create journal file");

                TryExec(() =>
                {
                    _journal = new FileStream(_journalFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, BasePage.PAGE_SIZE);
                });
            }

            _log.Write(Logger.JOURNAL, "write page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            // just write original bytes in order that are changed
            _journal.Write(buffer, 0, BasePage.PAGE_SIZE);

            _journalPages.Add(pageID);
        }

        /// <summary>
        /// Recovery journal file. This can happend when database initialize and in a rollback transaction.
        /// </summary>
        public void Recovery()
        {
            // check if exists journal file (if opended)
            if (_journal == null && File.Exists(_journalFilename))
            {
                TryExec(() =>
                {
                    _journal = new FileStream(_journalFilename, 
                        FileMode.Open, 
                        FileAccess.ReadWrite, FileShare.None, 
                        BasePage.PAGE_SIZE);
                });
            }

            // no journal, exit
            if (_journal == null) return;

            var fileSize = _stream.Length;
            var buffer = new byte[BasePage.PAGE_SIZE];

            // seek to begin file before start
            _journal.Seek(0, SeekOrigin.Begin);

            while (_journal.Position < _journal.Length)
            {
                // read page bytes from journal file
                _journal.Read(buffer, 0, BasePage.PAGE_SIZE);

                // read pageID (first 4 bytes)
                var pageID = BitConverter.ToUInt32(buffer, 0);

                _log.Write(Logger.RECOVERY, "recover page #{0:0000}", pageID);

                // if header, read all byte (to get original filesize)
                if (pageID == 0)
                {
                    var header = (HeaderPage)BasePage.ReadPage(buffer);

                    fileSize = BasePage.GetSizeOfPages(header.LastPageID + 1);
                }

                // write in stream
                this.WritePage(pageID, buffer);
            }

            _log.Write(Logger.RECOVERY, "resize datafile to {0} bytes", fileSize);

            // redim filesize if grow more than original before rollback
            _stream.SetLength(fileSize);

            // empty journal file
            this.ClearJournal();
        }

        /// <summary>
        /// Clear jounal file (set size to 0 length)
        /// </summary>
        public void ClearJournal()
        {
            if (_journal != null)
            {
                _log.Write(Logger.JOURNAL, "cleaning journal file");

                _journal.Seek(0, SeekOrigin.Begin);
                _journal.SetLength(0);
                _journalPages = new HashSet<uint>();
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Try run an operation over datafile - keep tring if locked
        /// </summary>
        private void TryExec(Action action)
        {
            var timer = DateTime.UtcNow.Add(_options.Timeout);
            var locked = false;

            while (DateTime.UtcNow < timer)
            {
                try
                {
                    action();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // This exception can occurs when a single instance release file but Windows Filesystem are not fully released.
                    if (locked == false)
                    {
                        locked = true;
                        _log.Write(Logger.ERROR, "unauthorized file access, waiting for {0} timeout", _options.Timeout);
                    }

                    IOExceptionExtensions.WaitFor(250);
                }
                catch (IOException ex)
                {
                    if (locked == false)
                    {
                        locked = true;
                        _log.Write(Logger.ERROR, "locked file, waiting for {0} timeout", _options.Timeout);
                    }

                    ex.WaitIfLocked(250);
                }
            }

            _log.Write(Logger.ERROR, "timeout disk access after {0}", _options.Timeout);

            throw LiteException.LockTimeout(_options.Timeout);
        }

        #endregion

    }
}