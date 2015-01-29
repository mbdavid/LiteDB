using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal class DiskService : IDisposable
    {
        private const int LOCK_POSITION = 0;

        private ConnectionString _connectionString;

        private BinaryReader _reader;
        private BinaryWriter _writer;

        public DiskService(ConnectionString connectionString)
        {
            _connectionString = connectionString;

            // Open file as ReadOnly - if we need use Write, re-open in Write Mode
            var stream = File.Open(_connectionString.Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            _reader = new BinaryReader(stream);
        }

        /// <summary>
        /// Create a new Page instance and read data from disk
        /// </summary>
        public T ReadPage<T>(uint pageID)
            where T : BasePage, new()
        {
            // create page instance and read from disk (read page header + content page)
            var page = new T();

            this.TryExec(() =>
            {
                // position cursor
                _reader.Seek(pageID * BasePage.PAGE_SIZE);

                // target = it's the target position after reader header. It's used when header does not conaints all PAGE_HEADER_SIZE
                var target = _reader.BaseStream.Position + BasePage.PAGE_HEADER_SIZE;

                // read page header
                page.ReadHeader(_reader);

                // if T is base and PageType has a defined type, convert page
                var isBase = page.GetType() == typeof(BasePage);

                if (page.PageType == PageType.Index && isBase) page = (T)(object)page.CopyTo<IndexPage>();
                else if (page.PageType == PageType.Data && isBase) page = (T)(object)page.CopyTo<DataPage>();
                else if (page.PageType == PageType.Extend && isBase) page = (T)(object)page.CopyTo<ExtendPage>();
                else if (page.PageType == PageType.Collection && isBase) page = (T)(object)page.CopyTo<CollectionPage>();

                // read page content if page is not empty
                if (page.PageType != PageType.Empty)
                {
                    // position reader to the end of page header
                    _reader.BaseStream.Seek(target - _reader.BaseStream.Position, SeekOrigin.Current);

                    // read page content
                    page.ReadContent(_reader);
                }
            });

            return page;
        }

        /// <summary>
        /// Write a page from memory to disk 
        /// </summary>
        public void WritePage(BasePage page)
        {
            DiskService.WritePage(GetWriter(), page);
        }

        /// <summary>
        /// Static method for write a page using a diferent writer - used when create empty datafile
        /// </summary>
        public static void WritePage(BinaryWriter writer, BasePage page)
        {
            // Position cursor
            writer.Seek(page.PageID * BasePage.PAGE_SIZE);

            // target = it's the target position after write header. It's used when header does not conaints all PAGE_HEADER_SIZE
            var target = writer.BaseStream.Position + BasePage.PAGE_HEADER_SIZE;

            // Write page header
            page.WriteHeader(writer);

            // write content except for empty pages
            if (page.PageType != PageType.Empty)
            {
                // position writer to the end of page header
                writer.BaseStream.Seek(target - writer.BaseStream.Position, SeekOrigin.Current);

                page.WriteContent(writer);
            }

            // if page is dirty, clean up
            page.IsDirty = false;
        }

        /// <summary>
        /// Pre-allocate more disk space to fast write new pages on disk
        /// </summary>
        public void AllocateDiskSpace(long length)
        {
            var writer = this.GetWriter();
            var stream = writer.BaseStream as FileStream;

            if (length > stream.Length)
            {
                stream.SetLength(length);
            }
        }

        /// <summary>
        /// Get BinaryWriter
        /// </summary>
        private BinaryWriter GetWriter()
        {
            // If no writer - re-open file in Write Mode
            if (_writer == null)
            {
                _reader.Close(); // Close reader

                var stream = File.Open(_connectionString.Filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                _reader = new BinaryReader(stream);
                _writer = new BinaryWriter(stream);
            }

            return _writer;
        }

        #region Lock/Unlock functions

        /// <summary>
        /// Lock the datafile when start a begin transaction
        /// </summary>
        public void Lock()
        {
            var stream = this.GetWriter().BaseStream as FileStream;

            this.TryExec(() =>
            {
                // try to lock - if is in use, a exception will be throwed
                stream.Lock(LOCK_POSITION, 1);

            });
        }

        /// <summary>
        /// Unlock the datafile
        /// </summary>
        public void UnLock()
        {
            var stream = this.GetWriter().BaseStream as FileStream;

            stream.Unlock(LOCK_POSITION, 1);
        }

        public void Flush()
        {
            this.GetWriter().BaseStream.Flush();
        }

        /// <summary>
        /// Lock all file during write operations - avoid reads during inconsistence data
        /// </summary>
        public void ProtectWriteFile(Action action)
        {
            var stream = this.GetWriter().BaseStream as FileStream;
            var fileLength = stream.Length;

            stream.Lock(LOCK_POSITION + 1, fileLength);

            action();

            stream.Unlock(LOCK_POSITION + 1, fileLength);
        }

        /// <summary>
        /// Try execute a block of code until timeout when IO lock exception occurs
        /// </summary>
        public void TryExec(Action action)
        {
            var timeout = DateTime.Now.Add(_connectionString.Timeout);

            while (DateTime.Now < timeout)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(250);
                }
            }

            throw new ApplicationException("Connection timeout. Datafile has locked for too long.");
        }

        #endregion

        public void Dispose()
        {
            _reader.Close();

            if (_writer != null)
            {
                _writer.Close();
            }
        }
    }
}
