using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LiteDB
{
    internal class FileDiskService : IDiskService
    {
        /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        private FileStream _stream;
        private string _filename;
        private long _lockLength;

        private string _tempFilename;
        private FileStream _journal;
        private string _journalFilename;
        private bool _journalEnabled;
        private HashSet<uint> _journalPages = new HashSet<uint>();

        private Logger _log;
        private TimeSpan _timeout;
        private bool _readonly;
        private long _initialSize;
        private long _limitSize;

        #region Initialize disk

        public FileDiskService(ConnectionString conn, Logger log)
        {
            _filename = conn.GetValue<string>("filename", "");
            var journalEnabled = conn.GetValue<bool>("journal", true);
            _timeout = conn.GetValue<TimeSpan>("timeout", new TimeSpan(0, 1, 0));
            _readonly = conn.GetValue<bool>("readonly", false);
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

            _journalEnabled = _readonly ? false : journalEnabled; // readonly? no journal
            _journalFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-journal" + Path.GetExtension(_filename));
            _tempFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-temp" + Path.GetExtension(_filename));
        }

        /// <summary>
        /// Open datafile - returns true if new
        /// </summary>
        public bool Initialize()
        {
            _log.Write(Logger.DISK, "open datafile '{0}', page size {1}", Path.GetFileName(_filename), BasePage.PAGE_SIZE);

            // open data file (r/w or r)
            _stream = new FileStream(_filename,
                _readonly ? FileMode.Open : FileMode.OpenOrCreate,
                _readonly ? FileAccess.Read : FileAccess.ReadWrite,
                _readonly ? FileShare.Read : FileShare.ReadWrite,
                BasePage.PAGE_SIZE);

            if (_stream.Length == 0)
            {
                _log.Write(Logger.DISK, "initialize new datafile");

                // if has a initial size, reserve this space
                if (_initialSize > 0)
                {
                    _log.Write(Logger.DISK, "initial datafile size {0}", _initialSize);

                    _stream.SetLength(_initialSize);
                }

                return true;
            }
            else
            {
                this.TryRecovery();
                return false;
            }
        }

        /// <summary>
        /// Create new database - just create empty header page
        /// </summary>
        public virtual void CreateNew()
        {
            this.WritePage(0, new HeaderPage().WritePage());
        }

        #endregion Initialize disk

        #region Lock/Unlock

        /// <summary>
        /// Lock datafile agains other process read/write
        /// </summary>
        public void Lock()
        {
            this.TryExec(() =>
            {
                _lockLength = _stream.Length;
                _log.Write(Logger.DISK, "lock datafile");
                _stream.Lock(0, _lockLength);
            });
        }

        /// <summary>
        /// Release lock
        /// </summary>
        public void Unlock()
        {
            _log.Write(Logger.DISK, "unlock datafile");
            _stream.Unlock(0, _lockLength);
        }

        #endregion Lock/Unlock

        #region Read/Write

        /// <summary>
        /// Read first 2 bytes from datafile - contains changeID (avoid to read all header page)
        /// </summary>
        public ushort GetChangeID()
        {
            var bytes = new byte[2];

            this.TryExec(() =>
            {
                _stream.Seek(HeaderPage.CHANGE_ID_POSITION, SeekOrigin.Begin);
                _stream.Read(bytes, 0, 2);
            });

            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public virtual byte[] ReadPage(uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            this.TryExec(() =>
            {
                // position cursor
                if (_stream.Position != position)
                {
                    _stream.Seek(position, SeekOrigin.Begin);
                }

                // read bytes from data file
                _stream.Read(buffer, 0, BasePage.PAGE_SIZE);
            });

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
            if (_limitSize > 0 && fileSize > _limitSize) throw LiteException.FileSizeExceeds(_limitSize);

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

                    _journal = new FileStream(_journalFilename,
                        FileMode.Create,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        BasePage.PAGE_SIZE);
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
                this.TryExec(() => File.Delete(_journalFilename));
            }
        }

        public virtual void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
            }
        }

        #endregion Journal file

        #region Recovery datafile

        private void TryRecovery()
        {
            // if I can open journal file, test FINISH_POSITION. If no journal, do not call action()
            this.OpenExclusiveFile(_journalFilename, (journal) =>
            {
                _log.Write(Logger.RECOVERY, "journal file detected");

                // copy journal pages to datafile
                this.Recovery(journal);

                // close stream for delete file
                journal.Dispose();

                // delete journal - datafile finish
                this.TryExec(() => File.Delete(_journalFilename));

                _log.Write(Logger.RECOVERY, "recovery finish");
            });
        }

        private void Recovery(FileStream journal)
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
            File.Delete(_tempFilename);
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
                    Thread.Sleep(250);
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(250);
                }
            }

            _log.Write(Logger.ERROR, "timeout disk access after {0}", _timeout);

            throw LiteException.LockTimeout(_timeout);
        }

        private void OpenExclusiveFile(string filename, Action<FileStream> success)
        {
            // check if is using by another process, if not, call fn passing stream opened
            try
            {
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    success(stream);
                }
            }
            catch (Exception)
            {
                // not found OR lock by another process, .... no recovery, do nothing
            }
        }

        #endregion Utils
    }
}