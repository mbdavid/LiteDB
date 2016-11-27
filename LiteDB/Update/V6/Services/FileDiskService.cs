using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace LiteDB_V6
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

        private Logger _log;

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
    }
}