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

        private Logger _log; // will be initialize in "Initialize()"
        private FileOptions _options;

        #region Initialize/Dispose disk

        public FileDiskService(string filename)
            : this(filename, new FileOptions())
        {
        }

        public FileDiskService(string filename, FileOptions options)
        {
            // simple validations
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filename));
            if (options.InitialSize > options.LimitSize) throw new ArgumentException("limit size less than initial size");

            // setting class variables
            _filename = filename;
            _options = options;
        }

        public void Initialize(Logger log, string password)
        {
            // get log instance to disk
            _log = log;

            _log.Write(Logger.DISK, "open datafile '{0}'", Path.GetFileName(_filename));

            // open/create file using read only/exclusive options
            _stream = this.CreateFileStream(_filename,
                _options.ReadOnly ? System.IO.FileMode.Open : System.IO.FileMode.OpenOrCreate,
                _options.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.None);

            // if file is new, initialize
            if (_stream.Length == 0)
            {
                _log.Write(Logger.DISK, "initialize new datafile");

                // create datafile
                LiteEngine.CreateDatabase(_stream, password, _options.InitialSize);
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

            lock (_stream)
            {
                // position cursor
                if (_stream.Position != position)
                {
                    _stream.Seek(position, SeekOrigin.Begin);
                }

                // read bytes from data file
                _stream.Read(buffer, 0, BasePage.PAGE_SIZE);
            }

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

        /// <summary>
        /// Flush data from memory to disk
        /// </summary>
        public void Flush()
        {
            _log.Write(Logger.DISK, "flush data from memory to disk");

            _stream.Flush();
        }

        #endregion

        #region Create Stream

        /// <summary>
        /// Create a new filestream. Can be synced over async task (netstandard)
        /// </summary>
        private FileStream CreateFileStream(string path, System.IO.FileMode mode, FileAccess access, FileShare share)
        {
#if HAVE_SYNC_OVER_ASYNC
            if (_options.Async)
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