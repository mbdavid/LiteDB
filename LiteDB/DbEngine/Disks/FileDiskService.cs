using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement NTFS File disk
    /// </summary>
    internal class FileDiskService : IDiskService
    {
        /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        private Stream _stream;
        private string _filename;

        private string _tempFilename;
        private Stream _journal;
        private string _journalFilename;
        private bool _journalEnabled;
        private HashSet<uint> _journalPages = new HashSet<uint>();

        private Logger _log;
        private TimeSpan _timeout;
        private long _initialSize;
        private long _limitSize;

        private IFileHandler _fileHandler;

        #region Initialize disk

        public FileDiskService(ConnectionString conn, Logger log)
        {
            // setting all class variables
            _filename = conn.GetValue<string>("filename", "");
            _journalEnabled = conn.GetValue<bool>("journal", true);
            _timeout = conn.GetValue<TimeSpan>("timeout", new TimeSpan(0, 1, 0));
            _initialSize = conn.GetFileSize("initial size", 0);
            _limitSize = conn.GetFileSize("limit size", 0);
            var level = conn.GetValue<byte?>("log", null);

            // simple validations
            if (_filename.IsNullOrWhiteSpace()) throw new ArgumentNullException("filename");
            if (_initialSize > 0 && _initialSize < BasePage.GetSizeOfPages(10)) throw new ArgumentException("initial size too low");
            if (_limitSize > 0 && _limitSize < BasePage.GetSizeOfPages(10)) throw new ArgumentException("limit size too low");
            if (_initialSize > 0 && _limitSize > 0 && _initialSize > _limitSize) throw new ArgumentException("limit size less than initial size");

            // setup log + log-level
            _log = log;
            if (level.HasValue) _log.Level = level.Value;

            _journalFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-journal" + Path.GetExtension(_filename));
            _tempFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-temp" + Path.GetExtension(_filename));

            _fileHandler = LitePlatform.Platform.FileHandler;
        }

        /// <summary>
        /// Open datafile - returns true if new
        /// </summary>
        public bool Initialize()
        {
            var exists = _fileHandler.FileExists(_filename);

            if (exists) this.TryRecovery();

            return !exists;
        }

        /// <summary>
        /// Create new database - just create empty header page
        /// </summary>
        public void CreateNew()
        {
            // open file as create mode
            using (var stream = _fileHandler.CreateFile(_filename, false))
            {
                _log.Write(Logger.DISK, "initialize new datafile");

                // if has a initial size, reserve this space
                if (_initialSize > 0)
                {
                    _log.Write(Logger.DISK, "initial datafile size {0}", _initialSize);
                    stream.SetLength(_initialSize);
                }

                // create a new header page in bytes
                var bytes = this.CreateHeaderPage().WritePage();

                // write bytes on page
                stream.Write(bytes, 0, BasePage.PAGE_SIZE);
            }
        }

        /// <summary>
        /// To be override in Encripted disk
        /// </summary>
        protected virtual HeaderPage CreateHeaderPage()
        {
            return new HeaderPage();
        }

        /// <summary>
        /// To be override in Encripted disk
        /// </summary>
        protected virtual void ValidatePassword(byte[] passwordHash)
        {
            if (passwordHash.Any(b => b > 0))
            {
                throw LiteException.DatabaseWrongPassword();
            }
        }

        #endregion Initialize disk

        #region Open/Close

        /// <summary>
        /// Open datafile if not opened
        /// </summary>
        public void Open(bool readOnly)
        {
            // checked if database is open in read mode but needs be in write mode
            if (_stream != null && readOnly == false && _stream.CanWrite == false)
            {
                // close stream (will be open in write mode)
                _log.Write(Logger.DISK, "close read only datafile");
                _stream.Dispose();
                _stream = null;
            }

            // if stream are already opended stops
            if (_stream != null) return;

            // read = shared read
            // write = exclusive write

            _log.Write(Logger.DISK, "open {0} datafile '{1}', page size {2}", readOnly ? "read" : "write", Path.GetFileName(_filename), BasePage.PAGE_SIZE);

            TryExec(() =>
            {
                _stream = _fileHandler.OpenFileAsStream(_filename, readOnly);
            });
        }

        /// <summary>
        /// Close datafile
        /// </summary>
        public void Close()
        {
            if (_stream != null)
            {
                _log.Write(Logger.DISK, "close datafile '{0}'", Path.GetFileName(_filename));
                _stream.Dispose();
                _stream = null;
            }
        }

        public virtual void Dispose()
        {
            this.Close();
        }

        #endregion Open/Close

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

            // when read header, checks passoword
            if (pageID == 0)
            {
                // I know, header page will be double read (it's the price for isolated concerns)
                var header = (HeaderPage)BasePage.ReadPage(buffer);
                ValidatePassword(header.Password);
            }

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
            if (_limitSize > 0 && fileSize > _limitSize) throw LiteException.FileSizeExceeded(_limitSize);

            // fileSize parameter tell me final size of data file - helpful to extend first datafile
            _stream.SetLength(fileSize);
        }

        #endregion Read/Write

        #region Journal file

        public void WriteJournal(uint pageID, byte[] buffer)
        {
            if (_journalEnabled == false) return;

            // test if this page is not in journal file
            if (_journalPages.Contains(pageID)) return;

            // open journal file if not used yet
            if (_journal == null)
            {
                // open journal file in EXCLUSIVE mode
                this.TryExec(() =>
                {
                    _log.Write(Logger.JOURNAL, "create journal file");

                    _journal = _fileHandler.CreateFile(_journalFilename, true);
                });
            }

            _log.Write(Logger.JOURNAL, "write page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            // just write original bytes in order that are changed
            _journal.Write(buffer, 0, BasePage.PAGE_SIZE);

            _journalPages.Add(pageID);
        }

        public void DeleteJournal()
        {
            if (_journalEnabled == false) return;

            if (_journal != null)
            {
                _log.Write(Logger.JOURNAL, "delete journal file");

                // clear pages in journal file
                _journalPages.Clear();

                // close journal stream and delete file
                _journal.Dispose();
                _journal = null;

                // remove journal file
                this.TryExec(() => _fileHandler.DeleteFile(_journalFilename));
            }
        }

        #endregion Journal file

        #region Recovery datafile

        private void TryRecovery()
        {
            if (!_journalEnabled) return;

            // avoid debug window always throw an exception if file didn't exists
            if (!_fileHandler.FileExists(_journalFilename)) return;

            // if I can open journal file, test FINISH_POSITION. If no journal, do not call action()
            _fileHandler.OpenExclusiveFile(_journalFilename, (journal) =>
            {
                this.Open(false);

                _log.Write(Logger.RECOVERY, "journal file detected");

                // copy journal pages to datafile
                this.Recovery(journal);

                // close stream for delete file
                journal.Dispose();

                // delete journal - datafile finish
                this.TryExec(() => _fileHandler.DeleteFile(_journalFilename));

                _log.Write(Logger.RECOVERY, "recovery finish");

                this.Close();
            });
        }

        private void Recovery(Stream journal)
        {
            var fileSize = _stream.Length;
            var buffer = new byte[BasePage.PAGE_SIZE];

            journal.Seek(0, SeekOrigin.Begin);

            while (journal.Position < journal.Length)
            {
                // read page bytes from journal file
                journal.Read(buffer, 0, BasePage.PAGE_SIZE);

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
        }

        #endregion Recovery datafile

        #region Temporary

        public IDiskService GetTempDisk()
        {
            // if exists, delete first
            this.DeleteTempDisk();

            // no journal, no logger
            return new FileDiskService(new ConnectionString("filename=" + _tempFilename + ";journal=false"), new Logger());
        }

        public void DeleteTempDisk()
        {
            _fileHandler.DeleteFile(_tempFilename);
        }

        #endregion Temporary

        #region Utils

        /// <summary>
        /// Try run an operation over datafile - keep tring if locked
        /// </summary>
        private void TryExec(Action action)
        {
            var timer = DateTime.UtcNow.Add(_timeout);

            while (DateTime.UtcNow < timer)
            {
                try
                {
                    action();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    LitePlatform.Platform.WaitFor(250);
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(250);
                }
            }

            _log.Write(Logger.ERROR, "timeout disk access after {0}", _timeout);

            throw LiteException.LockTimeout(_timeout);
        }

        #endregion Utils
    }
}